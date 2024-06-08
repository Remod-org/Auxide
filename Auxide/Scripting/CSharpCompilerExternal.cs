using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Auxide.Scripting
{
    internal static class CSharpCompilerExternal
    {
        private static int _counter = 0;
        private static Dictionary<string, string> referenceCache = new Dictionary<string, string>();
        private static string command;

        public static CompilationResult Build(string name, string code, string compileCmd = null, string path = null)
        {
            command = compileCmd;
            // Always logged
            if (Auxide.verbose) Utils.DoLog($"Entered Build({name})");

            //EmitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb, pdbFilePath: $"{Auxide.ScriptPath}/{name}/.pdb");
            SyntaxTree syntaxTree = ParseCode(code);
            List<string> output = new List<string>();

            if (compileCmd != null && path != null)
            {
                string _references = GetReferences(syntaxTree);
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        Chmod(compileCmd, "775");
                        break;
                }
                string outfile = Path.GetTempFileName();
                //string arguments = " /reference:" + Assembly.GetExecutingAssembly().Location + " " + path + $" -out:{outfile}.dll";

                string arguments = _references + " " + path + $" -nowarn:0414 -nowarn:0649 -out:{outfile}.{_counter++}.dll";
                Utils.DoLog($"Running: {compileCmd}{arguments}");
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = compileCmd,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    output.Add(proc.StandardOutput.ReadLine());
                }

                //File.Delete(compileCmd);

                if (File.Exists($"{outfile}.{_counter}.dll"))
                {
                    byte[] assemblyData = File.ReadAllBytes($"{outfile}.{_counter}.dll");
                    File.Delete($"{outfile}.{_counter}.dll");
                    return new CompilationResult(true, assemblyData, output);
                }
            }

            return new CompilationResult(false, null, new List<string>() { output.ToString() });
        }

        private static SyntaxTree ParseCode(string code)
        {
            SourceText sourceText = SourceText.From(code, Encoding.UTF8); // Added encoding to test
            //CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6);
            CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            //CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Script);
            ////CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.CSharp6, DocumentationMode.Parse, SourceCodeKind.Regular);
            //return SyntaxFactory.ParseSyntaxTree(sourceText, options);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText, options);
            if (Auxide.verbose)
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

        private static string GetReferenceAlt(string assemblyName, string path, bool global = false)
        {
            string main = global ? "global " : "";
            ref oldOrNew = ref CollectionsMarshal.GetValueRefOrAddDefault(referenceCache, assemblyName, out var exists);
            if (!exists)
            {
                if (Auxide.verbose) Utils.DoLog($"Loading new {main}reference to {path} for {assemblyName}", false);
                referenceCache.Add(assemblyName, path);
                return $" /reference:{path}";
            }
            if (Auxide.verbose) Utils.DoLog($"Loading cached {main}reference to {path} for {assemblyName}", false);
            return $" /reference:{referenceCache[assemblyName]}";
        }
        private static string GetReference(string assemblyName, string path, bool global = false)
        {
            string main = global ? "global " : "";
            if (referenceCache.ContainsKey(assemblyName))
            {
                if (Auxide.verbose) Utils.DoLog($"Loading cached {main}reference to {path} for {assemblyName}", false);
                return $" /reference:{referenceCache[assemblyName]}";
            }
            if (Auxide.verbose) Utils.DoLog($"Loading new {main}reference to {path} for {assemblyName}", false);
            referenceCache.Add(assemblyName, path);
            return $" /reference:{path}";
        }

        private static string GetReferences(SyntaxTree tree)
        {
            // For the command line compiler
            string references = "";
            //List<string> referenced = new List<string>() { "System", "Auxide", "Rust.Global", "Facepunch.System", "UnityEngine", "Assembly-CSharp"};
            List<string> referenced = new List<string>() { "System", "Auxide", "Rust.Global", "Facepunch.System", "Assembly-CSharp"};
            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            //outrefs += " /reference:" + assemblyPath + "/mscorlib.dll";
            //outrefs += " /reference:" + assemblyPath + "/Assembly-CSharp.dll";
            //outrefs += " /reference:" + assemblyPath + "/Facepunch.System.dll";
            //outrefs += " /reference:" + assemblyPath + "/Rust.Global.dll";
            ////outrefs += " /reference:" + assemblyPath + "/protobuf-net.dll";
            //outrefs += " /reference:" + AppContext.BaseDirectory + "/HarmonyMods/Auxide.dll";

            ////references += GetReference("System", typeof(object).Assembly.Location, true);
            //references += GetReference("Auxide", Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide.dll"), true);
            //references += GetReference("Rust.Global", Path.Combine(assemblyPath, "Rust.Global.dll"), true);
            //references += GetReference("Facepunch.System", Path.Combine(assemblyPath, "Facepunch.System.dll"), true);
            //references += GetReference("UnityEngine", Path.Combine(assemblyPath, "UnityEngine.dll"), true);
            //references += GetReference("Assembly-CSharp", Path.Combine(assemblyPath, "Assembly-CSharp.dll"), true);

            //references += GetReference("mscorlib", typeof(object).Assembly.Location, true);
            references += GetReference("Assembly-CSharp", Path.Combine(assemblyPath, "Assembly-CSharp.dll"), true);
            references += GetReference("Facepunch.System", Path.Combine(assemblyPath, "Facepunch.System.dll"), true);
            references += GetReference("Rust.Global", Path.Combine(assemblyPath, "Rust.Global.dll"), true);
            references += GetReference("Auxide", Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide.dll"), true);

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
                        if (referenced.Contains(className)) continue;
                        if (File.Exists(Path.Combine(assemblyPath, $"{className}.dll")))
                        {
                            Assembly assembly = Assembly.Load(className);
                            if (assembly != null)
                            {
                                references += GetReference(className, Path.Combine(assemblyPath, $"{className}.dll"));
                            }
                            referenced.Add(className);
                        }

                        // Now load the dll with the name This.dll in addition to This.That.dll.  Not perfect but also not the main source of pain so far.
                        string[] elem2 = elem[1].Trim().Split('.');
                        if (elem2.Length == 2)
                        {
                            string className2 = elem2[0].Trim();
                            if (referenced.Contains(className2)) continue;
                            if (File.Exists(Path.Combine(assemblyPath, $"{className2}.dll")))
                            {
                                Assembly assembly = Assembly.Load(className2);
                                if (assembly != null)
                                {
                                    references += GetReference(className2, Path.Combine(assemblyPath, $"{className2}.dll"));
                                }
                                referenced.Add(className);
                            }
                        }
                    }
                }
            }
            return references;
        }

        private static bool Chmod(string filePath, string permissions = "700", bool recursive = false)
        {
            string cmd;
            if (recursive)
            {
                cmd = $"chmod -R {permissions} {filePath}";
            }
            else
            {
                cmd = $"chmod {permissions} {filePath}";
            }

            try
            {
                using (Process proc = Process.Start("/bin/sh", $"-c \"{cmd}\""))
                {
                    proc.WaitForExit();
                    return proc.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
