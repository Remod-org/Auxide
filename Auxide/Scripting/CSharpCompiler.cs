using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using UnityEngine;

namespace Auxide.Scripting
{
    internal static class CSharpCompiler
    {
        private static readonly CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release)//, reportSuppressedDiagnostics: true)
            .WithPlatform(Platform.AnyCpu);
        //.WithOptimizationLevel(OptimizationLevel.Release);

        //.WithOverflowChecks(true)
        //.WithPlatform(Microsoft.CodeAnalysis.Platform.AnyCpu);
        //.WithPlatform(Microsoft.CodeAnalysis.Platform.AnyCpu32BitPreferred);
        //.WithUsings(DefaultNamespaces)
        //, platform: Microsoft.CodeAnalysis.Platform.AnyCpu, allowUnsafe: true);
        //optimizationLevel: OptimizationLevel.Debug);

        private static readonly EmitOptions EmitOptions = new EmitOptions(false, DebugInformationFormat.Embedded);//, tolerateErrors: true);
        //private static readonly EmitOptions EmitOptions = new EmitOptions(true, DebugInformationFormat.PortablePdb, includePrivateMembers: false);
        //private static readonly EmitOptions EmitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
        //private static readonly EmitOptions EmitOptions = new EmitOptions(true, DebugInformationFormat.PortablePdb);
        private static Dictionary<string, MetadataReference> referenceCache = new Dictionary<string, MetadataReference>();

        //private static int _counter = 0;

        public static CompilationResult Build(string name, string code, string compileCmd = null, string path = null)
        {
            if (Auxide.verbose) Utils.DoLog($"Entered Build({name})", false);
            string propertyInfo = "";
            //PropertyInfo[] properties = typeof(Environment).GetProperties(BindingFlags.Public | BindingFlags.Static);
            //foreach(var prop in properties)
            //{
            //    propertyInfo += $"{prop.Name}: {prop.GetValue(null)}\n";
            //}
            foreach(DictionaryEntry e in Environment.GetEnvironmentVariables())
            {
                propertyInfo += e.Key + ":" + e.Value + "\n";
            }
            if (Auxide.verbose) Utils.DoLog($"ENV:\n{propertyInfo}", false);

            SyntaxTree syntaxTree = ParseCode(code);
            SyntaxTree[] st = new SyntaxTree[] { syntaxTree };

            string assemblyName = $"{name}";//.{_counter++}";
            // With no references loaded (null), emitResult is actually set to false, and we get output about the failures.
            // With them loaded, emit bails/crashes, and we get no output.
            IEnumerable<MetadataReference> references = GetReferences(syntaxTree);

            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, st, references, CompilationOptions);
            if (Auxide.verbose) Utils.DoLog($"Compilation created for assemblyName {assemblyName}", false);

            using (MemoryStream ms = new MemoryStream())
            //using (MemoryStream symbols = new MemoryStream())
            {
                byte[] assemblyData = null;
                // Always logged
                if (Auxide.verbose) Utils.DoLog("pre-emit", false); // Tried removing emitoptions
                //var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb,
                //    pdbFilePath: "plugin.pdb");
                //EmitResult emitResult = compilation.Emit(peStream: ms, pdbStream: symbols, options: emitOptions);
                EmitResult emitResult = compilation.Emit(ms, options: EmitOptions);
                //assemblyData = ms.ToArray();
                //string outfile = Path.GetTempFileName();
                //File.WriteAllBytes(outfile, assemblyData);
                Utils.DoLog($"post-emit: success == {emitResult.Success}");

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
                    // Never logged
                    if (Auxide.verbose) Utils.DoLog($"Build({name}) succeeded!", false);
                    ms.Seek(0, SeekOrigin.Begin);
                    assemblyData = ms.ToArray();
                }

                List<string> errors = emitResult.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToList();
                return new CompilationResult(emitResult.Success, assemblyData, errors);
            }
        }

        private static SyntaxTree ParseCode(string code)
        {
            SourceText sourceText = SourceText.From(code);//, Encoding.UTF8); // Added encoding to test
            ////CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6);
            CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            ////CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Script);
            //CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.CSharp6, DocumentationMode.Parse, SourceCodeKind.Regular);
            //return SyntaxFactory.ParseSyntaxTree(sourceText, options);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText, options);//, path: "plugin.cs");
            if (Auxide.verbose)
            {
                // Nothing shown, tree appears to always be clean barring typical syntax errors, etc.
                IEnumerable<Diagnostic> diag = tree.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error);
                foreach (Diagnostic d in diag)
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

            //Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //foreach (Assembly assembly in assemblies)
            //{
            //    yield return GetReference(assembly.FullName, assemblyDir, true);
            //}

            if (!string.IsNullOrEmpty(path1))
            {
                yield return GetReference("mscorlib", path1, true);
                yield return GetReference("System", Path.Combine(assemblyDir, "System.dll"), true);
                yield return GetReference("System.Core", Path.Combine(assemblyDir, "System.Core.dll"), true);
                yield return GetReference("System.Net.Http", Path.Combine(assemblyDir, "System.Net.Http.dll"), true);
            }

            string path2 = Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide.dll");
            yield return GetReference("RustScript", path2, true);

            string path3 = typeof(Debug).Assembly.Location;
            if (!string.IsNullOrEmpty(path3)) yield return GetReference("UnityEngine", path3, true);

            string path4 = typeof(ServerMgr).Assembly.Location;
            if (!string.IsNullOrEmpty(path4)) yield return GetReference("Assembly-CSharp", path4, true);

            //Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            //    .Where(a => !a.IsDynamic)
            //    .Where(a => a.Location != "")
            //    .Where(a => a.GetName().Name != "")
            //    .ToArray();
            //foreach (Assembly y in assemblies)
            //{
            //    yield return GetReference(y.GetName().Name, y.Location);
            //}

            SyntaxNode[] usings = tree.GetRoot().DescendantNodes().Where(x => x.IsKind(SyntaxKind.UsingDirective)).ToArray();
            foreach (SyntaxNode us in usings)
            {
                SourceText t = us.GetText();

                string[] lines = t.ToString().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string newline = line.Replace(";", "");
                    string[] elem = newline.Split(' ');
                    if (elem.Length == 2)
                    {
                        string className = elem[1].Trim();
                        if (File.Exists($"{assemblyDir}/{className}.dll"))
                        {
                            Assembly assembly = Assembly.Load(className);
                            if (assembly != null)
                            {
                                if (!referenceCache.ContainsKey(className))
                                {
                                    yield return GetReference(className, $"{assemblyDir}/{className}.dll");
                                }
                            }
                        }

                        // Now load the dll with the name This.dll in addition to This.That.dll.  Not perfect but also not the main source of pain so far.
                        string[] elem2 = elem[1].Trim().Split('.');
                        if (elem2.Length == 2)
                        {
                            string className2 = elem2[0].Trim();
                            if (File.Exists($"{assemblyDir}/{className2}.dll"))
                            {
                                Assembly assembly = Assembly.Load(className2);
                                if (assembly != null)
                                {
                                    if (!referenceCache.ContainsKey(className2))
                                    {
                                        yield return GetReference(className2, $"{assemblyDir}/{className2}.dll");
                                    }
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
