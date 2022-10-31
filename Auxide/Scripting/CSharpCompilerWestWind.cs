using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using UnityEngine;
using Westwind.Scripting;

namespace Auxide.Scripting
{
    internal class CSharpCompiler
    {
        private static CSharpScriptExecution cse;
        private static Dictionary<string, string> referenceCache = new Dictionary<string, string>();

        public static CompilationResult Build(string name, string code, string compileCmd = null, string path = null)
        {
            List<string> errors = new List<string>();
            byte[] assemblyData;

            SyntaxTree syntaxTree = ParseCode(code);

            cse = new CSharpScriptExecution()
            {
                GeneratedClassName = name
            };

            GetReferences(syntaxTree);
            cse.CompileAssembly(code, true);
            Utils.DoLog(cse.ErrorMessage);
            errors[0] = cse.ErrorMessage;

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, cse.Assembly);
                assemblyData = stream.ToArray();
            }

            return new CompilationResult(true, assemblyData, errors);
        }

        private static SyntaxTree ParseCode(string code)
        {
            SourceText sourceText = SourceText.From(code);//, Encoding.UTF8); // Added encoding to test
            CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText, options);//, path: "plugin.cs");

            return tree;
        }

        private static void GetReference(string assemblyName, string path, bool global = false)
        {
            string main = global ? "global " : "";
            if (referenceCache.ContainsKey(assemblyName))
            {
                if (Auxide.verbose) Utils.DoLog($"Using cached {main}reference {assemblyName} from {path}", false);
            }

            if (Auxide.verbose) Utils.DoLog($"Loading new {main}reference {assemblyName} from {path}", false);
            cse.AddAssembly(path);
            referenceCache.Add(assemblyName, path);
        }

        private static void GetReferences(SyntaxTree tree)
        {
            //cse.AddDefaultReferencesAndNamespaces(); // This breaks when loading Microsoft.CSharp, which is not present.
            string path1 = typeof(object).Assembly.Location;
            string assemblyDir = Path.GetDirectoryName(path1);

            GetReference("mscorlib", path1, true);
            GetReference("System", Path.Combine(assemblyDir, "System.dll"), true);
            GetReference("System.Core", Path.Combine(assemblyDir, "System.Core.dll"), true);
            GetReference("Microsoft.CSharp", Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Microsoft.CSharp.dll"), true);
            GetReference("System.Net.Http", Path.Combine(assemblyDir, "System.Net.Http.dll"), true);
            //cse.AddAssembly(typeof(RustScript));
            GetReference("Auxide", Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide.dll"), true);
            //cse.AddAssembly(typeof(Debug));
            //cse.AddAssembly(typeof(ServerMgr));
            GetReference("UnityEngine", typeof(UnityEngine.Debug).Assembly.Location, true);
            GetReference("Assembly-CSharp", typeof(ServerMgr).Assembly.Location, true);

//            cse.AddNamespaces();

            SyntaxNode[] usings = tree.GetRoot().DescendantNodes().Where(x => x.IsKind(SyntaxKind.UsingDirective)).ToArray();
            SyntaxNode[] refs = tree.GetRoot().DescendantNodes().Where(x => x.IsKind(SyntaxKind.RefExpression)).ToArray();
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
                                    GetReference(className, $"{assemblyDir}/{className}.dll");
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
                                        GetReference(className2, $"{assemblyDir}/{className2}.dll");
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
            foreach (SyntaxNode us in refs)
            {
                SourceText t = us.GetText();

                string[] lines = t.ToString().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string newline = line.Replace(";", "");
                    string[] elem = newline.Split(':');
                    if (elem.Length == 2)
                    {
                        string nameSpace = elem[0].Trim();
                        if (File.Exists($"{assemblyDir}/{nameSpace}.dll"))
                        {
                            Utils.DoLog($"Adding namespace from reference: {nameSpace}");
                            cse.AddNamespace(nameSpace);
                        }
                    }
                }
            }
        }
    }
}
