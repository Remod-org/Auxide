using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Auxide.Scripting
{
    internal static class CSharpCompiler
    {
        private static ImmutableArray<byte> nums = new ImmutableArray<byte>();
        private static readonly CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary, false, null, null, null, null,
            OptimizationLevel.Release, false, true, null, null, nums, null,
            Platform.AnyCpu,
            ReportDiagnostic.Default, 4, null, true, true, null, null, null, null, null, false,
            MetadataImportOptions.Public, NullableContextOptions.Disable);

        private static readonly EmitOptions EmitOptions = new EmitOptions(false, DebugInformationFormat.Embedded);//, tolerateErrors: true);
        private static Dictionary<string, MetadataReference> referenceCache = new Dictionary<string, MetadataReference>();

        public static CompilationResult Build(string name, string code, string compileCmd = null, string path = null)
        {
            if (Auxide.verbose) Utils.DoLog($"Entered Build({name})", false);

            SyntaxTree syntaxTree = ParseCode(code);
            SyntaxTree[] st = new SyntaxTree[] { syntaxTree };

            string assemblyName = $"{name}";//.{_counter++}";
            IEnumerable<MetadataReference> references = GetReferences(syntaxTree);

            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, st, references, CompilationOptions);
            if (Auxide.verbose) Utils.DoLog($"Compilation created for assemblyName {assemblyName}", false);

            using (MemoryStream ms = new MemoryStream())
            {
                byte[] assemblyData = null;
                //if (Auxide.verbose) Utils.DoLog("pre-emit", false);
                CancellationToken cancellationToken = new CancellationToken();
                EmitResult emitResult = compilation.Emit(ms, null, null, null, null, EmitOptions, null, null, null, null, cancellationToken);
                //Utils.DoLog($"post-emit: success == {emitResult.Success}");

                if (!emitResult.Success)
                {
                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Utils.DoLog(string.Format("{0}: {1}, {2}", diagnostic.Id, diagnostic.GetMessage(), diagnostic.Location));
                    }
                }
                else
                {
                    if (Auxide.verbose) Utils.DoLog($"Build({name}) succeeded!", false);
                    ms.Seek(0, SeekOrigin.Begin);
                    assemblyData = ms.ToArray();
                }

                List<string> errors = emitResult.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToList();

                // Add reference for the compiled dll image
                RemoveReference(name);
                referenceCache.Add(name, MetadataReference.CreateFromStream(ms));
                return new CompilationResult(emitResult.Success, assemblyData, errors);
            }
        }

        public static void RemoveReference(string name)
        {
            // Typically for plugin unload
            if (referenceCache.ContainsKey(name))
            {
                if (Auxide.verbose) Utils.DoLog($"Removing reference for {name}");
                referenceCache.Remove(name);
            }
        }

        public static void ListReferences()
        {
            Utils.DoLog("List of collected references");
            int i = 1;
            foreach (KeyValuePair<string, MetadataReference> reference in referenceCache)
            {
                Utils.DoLog($"  {i}: {reference.Key}");
                i++;
            }
        }

        private static SyntaxTree ParseCode(string code)
        {
            SourceText sourceText = SourceText.From(code);//, Encoding.UTF8); // Added encoding to test
            CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText, options);//, path: "plugin.cs");
            if (Auxide.verbose)
            {
                // Nothing shown, tree appears to always be clean barring typical syntax errors, etc.
                foreach (Diagnostic d in tree.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error))
                {
                    Utils.DoLog("Source file syntax error - " + d.ToString() + ": " + d.GetMessage(), false);
                }
            }
            return tree;
        }

        private static MetadataReference GetReference(string assemblyName, string path, bool global = false)
        {
            string main = global ? "global " : "";
            if (referenceCache.ContainsKey(assemblyName))
            {
                if (Auxide.verbose) Utils.DoLog($"Loading cached {main}reference to {path} for {assemblyName}", false);
                return referenceCache[assemblyName];
            }

            if (Auxide.verbose) Utils.DoLog($"Loading new {main}reference to {path} for {assemblyName}", false);
            MetadataReference newref = MetadataReference.CreateFromFile(path);
            referenceCache.Add(assemblyName, newref);
            return newref;
        }

        private static IEnumerable<MetadataReference> GetReferences(SyntaxTree tree)
        {
            string path1 = typeof(object).Assembly.Location;
            string assemblyDir = Path.GetDirectoryName(path1);

            if (!string.IsNullOrEmpty(path1))
            {
                // Load references for all DLL files in Managed folder.
                foreach (string file in Directory.GetFiles(assemblyDir, "*.dll"))
                {
                    yield return MetadataReference.CreateFromFile(file);
                }
            }

            string path2 = Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide.dll");
            yield return GetReference("RustScript", path2, true);

            //string path3 = typeof(Debug).Assembly.Location;
            //if (!string.IsNullOrEmpty(path3)) yield return GetReference("UnityEngine", path3, true);

            //string path4 = typeof(ServerMgr).Assembly.Location;
            //if (!string.IsNullOrEmpty(path4)) yield return GetReference("Assembly-CSharp", path4, true);

            foreach (SyntaxNode us in tree.GetRoot().DescendantNodes().Where(x => x.IsKind(SyntaxKind.UsingDirective)).ToArray())
            {
                SourceText t = us.GetText();

                foreach (string line in t.ToString().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string newline = line.Replace(";", "");
                    string[] elem = newline.Split(' ');
                    if (elem.Length == 0) continue;
                    if (elem[0] != "using") continue;

                    if (Auxide.verbose) Utils.DoLog($"Checking for '{elem[1]?.Trim()}' in {assemblyDir}", false);
                    if (elem.Length == 2)
                    {
                        string className = elem[1].Trim();
                        if (File.Exists($"{assemblyDir}/{className}.dll"))
                        {
                            if (Auxide.verbose) Utils.DoLog($"Loading: '{className}'", false);
                            Assembly assembly = Assembly.Load(className);
                            if (assembly != null && !referenceCache.ContainsKey(className))
                            {
                                yield return GetReference(className, $"{assemblyDir}/{className}.dll");
                            }
                        }

                        // Now load the dll with the name This.dll in addition to This.That.dll.  Not perfect but also not the main source of pain so far.
                        string[] elem2 = elem[1].Trim().Split('.');
                        if (elem2.Length == 2)
                        {
                            string className2 = elem2[0].Trim();
                            if (File.Exists($"{assemblyDir}/{className2}.dll"))
                            {
                                if (Auxide.verbose) Utils.DoLog($"Loading: '{className2}'", false);
                                Assembly assembly = Assembly.Load(className2);
                                if (assembly != null && !referenceCache.ContainsKey(className2))
                                {
                                    yield return GetReference(className2, $"{assemblyDir}/{className2}.dll");
                                }
                            }
                        }
                    }
                }
            }

            // TODO: more
            // TODO: extract inter-script dependencies from the SyntaxTree?
            //foreach (SyntaxNode node in tree.GetRoot().DescendantNodes().Where(x => x.IsKind(SyntaxKind.ParameterList)).ToArray())
            //{
            //    SourceText t = node.GetText();
            //    string[] lines = t.ToString().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            //    foreach (string line in lines)
            //    {
            //        string newline = line.Replace(";", "");
            //        string[] elem = newline.Split(' ');
            //        if (elem.Length == 2)
            //        {
            //        }
            //    }
            //}
        }
    }
}
