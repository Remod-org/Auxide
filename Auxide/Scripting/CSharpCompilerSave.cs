using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Rust.ModLoader.Scripting
{
    internal static class CSharpCompilerSave
    {
        private static readonly CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary, reportSuppressedDiagnostics: true)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithOverflowChecks(true)
            .WithPlatform(Microsoft.CodeAnalysis.Platform.AnyCpu);
            //.WithUsings(DefaultNamespaces)
        //, platform: Microsoft.CodeAnalysis.Platform.AnyCpu, allowUnsafe: true);
        //optimizationLevel: OptimizationLevel.Debug);

        //private static readonly EmitOptions EmitOptions = new EmitOptions(false, DebugInformationFormat.Embedded);
        private static readonly EmitOptions EmitOptions = new EmitOptions(true, DebugInformationFormat.PortablePdb, includePrivateMembers: false);
        //private static readonly EmitOptions EmitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
        //private static readonly EmitOptions EmitOptions = new EmitOptions(true, DebugInformationFormat.PortablePdb);
        private static Dictionary<string, MetadataReference> referenceCache = new Dictionary<string, MetadataReference>();

        private static int _counter = 0;
        private static readonly bool verbose = Convert.ToBoolean(ModLoader.config["VerboseLogging"].Value);

        public static CompilationResult Build(string name, string code)
        {
            // Always logged
            if (verbose) Utils.DoLog($"Entered Build({name})");

            //EmitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb, pdbFilePath: $"{ModLoader.ScriptPath}/{name}/.pdb");
            SyntaxTree syntaxTree = ParseCode(code);
            //if (verbose)
            //{
            //    TreeWalk walker = new TreeWalk();
            //    walker.Visit(syntaxTree.GetRoot());
            //}

            IEnumerable<MetadataReference> references = GetReferences(syntaxTree);
            string assemblyName = $"{name}.{_counter++}";
            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, new SyntaxTree[] { syntaxTree }, ReferenceAssemblies.NetStandard20, CompilationOptions);
            compilation = compilation.AddReferences(references);
            //compilation = compilation.AddReferences(ReferenceAssemblies.Net461);
            //foreach (MetadataReference s in compilation.References)
            //{
            //    foreach (var a in s.Properties.Aliases)
            //    {
            //        Utils.DoLog($"Loaded reference: {a}");
            //    }
            //}
            //compilation.Emit("/tmp/tmp.dll"); // Creates empty file
            if (verbose) Utils.DoLog(compilation.SyntaxTrees[0].ToString());

            using (MemoryStream ms = new MemoryStream())
            //using (MemoryStream pdb = new MemoryStream())
            {
                // Always logged
                if (verbose) Utils.DoLog("pre-emit"); // Tried removing emitoptions
                //EmitResult emitResult = compilation.Emit(ms, pdb, options: EmitOptions);
                try
                {
                    EmitResult emitResult = compilation.Emit(ms, options: EmitOptions);
                    // Permanent error:
                    // Script 'XXXX' threw in Update: System.BadImageFormatException: Method has no body
                    // Never logged
                    if (verbose) Utils.DoLog("post-emit");

                    byte[] assemblyData = null;
                    if (emitResult.Success)
                    {
                        // Never logged
                        if (verbose) Utils.DoLog($"Build({name}) succeeded!");
                        ms.Seek(0, SeekOrigin.Begin);
                        assemblyData = ms.ToArray();
                    }
                    else if (verbose)
                    {
                        // Never logged
                        Utils.DoLog($"Build({name}) failed.  See main logfile for details.");
                    }

                    List<string> errors = emitResult.Diagnostics
                        .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString())
                        .ToList();

                    return new CompilationResult(emitResult.Success, assemblyData, errors);
                }
                catch (Exception ex)
                {
                    if (verbose) Utils.DoLog("EMIT ERROR: " + ex.ToString());
                    return new CompilationResult(false, null, new List<string>() { ex.ToString() });
                }
            }
        }

        private static SyntaxTree ParseCode(string code)
        {
            SourceText sourceText = SourceText.From(code, Encoding.UTF8); // Added encoding to test
            //CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6);
            //CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            //CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Script);
            CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.CSharp6, DocumentationMode.Parse, SourceCodeKind.Regular);
            //return SyntaxFactory.ParseSyntaxTree(sourceText, options);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText, options);
            if (verbose)
            {
                // Nothing shown, tree appears to always be clean barring typical syntax errors, etc.
                IEnumerable<Diagnostic> diag = tree.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error);
                foreach (Diagnostic d in diag)
                {
                    Utils.DoLog("Source file syntax error - " + d.ToString() + ": " + d.GetMessage());
                }
            }
            return tree;
        }

        private static MetadataReference GetReference(string assemblyName, string path, bool global = false)
        {
            string main = global ? "global " : "";
            if (referenceCache.ContainsKey(assemblyName))
            {
                if (verbose) Utils.DoLog($"Loading cached {main}reference to {path} for {assemblyName}");
                return referenceCache[assemblyName];
            }
            else
            {
                if (verbose) Utils.DoLog($"Loading new {main}reference to {path} for {assemblyName}");
                MetadataReference newref = MetadataReference.CreateFromFile(path);
                referenceCache.Add(assemblyName, newref);
                return newref;
            }
        }

        private static IEnumerable<MetadataReference> GetReferences(SyntaxTree tree)
        {
            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            yield return GetReference("mscorlib", typeof(object).Assembly.Location, true);
            yield return GetReference("System", assemblyPath + "/System.dll", true);
            yield return GetReference("RustScript", typeof(RustScript).Assembly.Location, true);
            //yield return GetReference("UnityEngine", typeof(UnityEngine.Debug).Assembly.Location, true);
            //yield return GetReference("Assembly-CSharp", typeof(ServerMgr).Assembly.Location, true);

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
                        if (File.Exists($"{assemblyPath}/{className}.dll"))
                        {
                            Assembly assembly = Assembly.Load(className);
                            if (assembly != null)
                            {
                                yield return GetReference(className, $"{assemblyPath}/{className}.dll");
                            }
                        }

                        // Now load the dll with the name This.dll in addition to This.That.dll.  Not perfect but also not the main source of pain so far.
                        string[] elem2 = elem[1].Trim().Split('.');
                        if (elem2.Length == 2)
                        {
                            string className2 = elem2[0].Trim();
                            if (File.Exists($"{assemblyPath}/{className2}.dll"))
                            {
                                Assembly assembly = Assembly.Load(className2);
                                if (assembly != null)
                                {
                                    yield return GetReference(className2, $"{assemblyPath}/{className2}.dll");
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
