using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Auxide.Exceptions;
using Auxide.Scripting;
using UnityEngine;

namespace Auxide
{
    internal partial class Script : IEquatable<Script>, IDisposable
    {
        public ScriptManager Manager { get; }
        public string Name { get; }
        public string Path { get; private set; }
        public string ConfigPath { get; private set; }
        public string DataPath { get; private set; }
        public string LangPath { get; private set; }
        public string SourceCode { get; private set; }
        public Assembly Assembly { get; private set; }
        public RustScript Instance { get; private set; }
        public HashSet<string> SoftDependencies { get; }
        public DynamicConfigFile config { get; private set; }
        public DataFileSystem data { get; private set; }

        //public string compileProc;

        public bool initialized;
        public Script(ScriptManager manager, string name)
        {
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SoftDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            Manager.ScriptUnloading(this);

            try
            {
                Instance?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"{Instance?.GetType().FullName}::Dispose threw: {e}");
            }

            Instance = null;
            Assembly = null;

            Manager.ScriptUnloaded(this);
            //Debug.LogWarning($"Script {Assembly.Location} unloaded...");
        }

        internal void Update(string path = null)
        {
            if (path == null) return;
            string code = "";

            //if (Auxide.config.Options.cSharpScripts)
            //{
            //    try
            //    {
            //        Utils.DoLog($"Trying to load code from {path}");
            //        code = File.ReadAllText(path);
            //    }
            //    catch (Exception e)
            //    {
            //        Utils.DoLog($"Failed to load script code at path: {path}: {e}");
            //        throw new ScriptLoadException(Name, $"Failed to load script code at path: {path}", e);
            //    }

            //    if (code != SourceCode)
            //    {
            //        if (!Auxide.useInternal)
            //        {
            //            GetCompiler();
            //        }
            //        Compile(path, code);
            //    }
            //}
            //else
            //{
            try
            {
                Utils.DoLog($"Trying to load dll from {path}");
                Assembly assembly = Assembly.LoadFile(path);
                Utils.DoLog($"Loaded assembly!");

                Initialize(path, code, assembly);
                initialized = true;
            }
            catch (Exception e)
            {
                initialized = false;
                Utils.DoLog($"Failed to load dll at path: {path}: {e}");
                throw new ScriptLoadException(Name, $"Failed to load dll at path: {path}", e);
            }
            //}
        }

        //private void GetCompiler()
        //{
        //    if (Auxide.useInternal) return;

        //    string proc = "csc.exe";
        //    switch (Environment.OSVersion.Platform)
        //    {
        //        case PlatformID.Unix:
        //        case PlatformID.MacOSX:
        //            proc = "mcs";
        //            break;
        //    }
        //    compileProc = System.IO.Path.Combine(Auxide.BinPath, proc);
        //    using (WebClient client = new WebClient())
        //    {
        //        client.DownloadFile($"https://code.remod.org/{proc}", compileProc);
        //    }
        //}

        //private void Compile(string path, string code)
        //{
        //    Utils.DoLog($"Compiling {Name} @ {path}");

        //    CompilationResult result = null;
        //    if (Auxide.useInternal)
        //    {
        //        result = CSharpCompiler.Build(Name, code, compileProc, path);
        //    }
        //    else
        //    {
        //        result = CSharpCompilerExternal.Build(Name, code, compileProc, path);
        //    }

        //    if (!result.IsSuccess)
        //    {
        //        string errorList = string.Join("\n", result.Errors);
        //        Utils.DoLog($"Script compile failed:\n{errorList}");
        //        throw new ScriptLoadException(Name, $"Script compile failed:\n{errorList}");
        //    }

        //    Assembly assembly;

        //    try
        //    {
        //        assembly = Assembly.Load(result.AssemblyData);
        //        Utils.DoLog($"Loaded assembly!");
        //    }
        //    catch (Exception e)
        //    {
        //        Utils.DoLog($"Failed to load assembly: {e}");
        //        throw new ScriptLoadException(Name, "Failed to load assembly", e);
        //    }

        //    Utils.DoLog($"Initializing {path}");
        //    Initialize(path, code, assembly);
        //}

        private void Initialize(string path, string code, Assembly assembly)
        {
            Dispose();
            Utils.DoLog($"Initializing assembly");

            Type type = assembly.GetType(Name);

            if (type == null)
            {
                Utils.DoLog($"Unable to find class '{Name}' in the compiled script.  Found: {type}");
                throw new ScriptLoadException(Name, $"Unable to find class '{Name}' in the compiled script.");
            }

            object instance;

            try
            {
                Utils.DoLog($"Attempting to create instance of type {type}");
                instance = Activator.CreateInstance(type);// as RustScript;
                Utils.DoLog($"Created instance of type {type}");
            }
            catch (Exception e)
            {
                Utils.DoLog($"Exception thrown in script '{Name}' constructor.");
                throw new ScriptLoadException(Name, $"Exception thrown in script '{Name}' constructor.", e);
            }

            RustScript scriptInstance = instance as RustScript;
            if (scriptInstance == null)
            //if (!(instance is RustScript scriptInstance))
            {
                Utils.DoLog($"Script class ({type.FullName}) must derive from RustScript.  Found: {instance.GetType()}");
                throw new ScriptLoadException(Name, $"Script class ({type.FullName}) must derive from RustScript.  Found: {instance.GetType()}");
            }

            scriptInstance.Manager = Manager;

            Assembly = assembly;
            Instance = scriptInstance;
            Path = path;
            SourceCode = code;

            string BaseName = Name.Replace(".dll", "");
            //if (Auxide.config.Options.cSharpScripts)
            //{
            //    BaseName = Name.Replace(".cs", "");
            //}

            ConfigPath = System.IO.Path.Combine(Auxide.ConfigPath, $"{BaseName}.json");
            DataPath = System.IO.Path.Combine(Auxide.DataPath, BaseName);
            // Yes, this will need to be extended...
            LangPath = System.IO.Path.Combine(Auxide.LangPath, "en");//, $"{BaseName}.json");

            scriptInstance.config = new DynamicConfigFile(ConfigPath);
            scriptInstance.data = new DataFileSystem(DataPath);
            scriptInstance.lang = new LangFileSystem(LangPath);

            SoftDependencies.Clear();
            List<string> allSoftReferences = Manager.PopulateScriptReferences(Instance).ToList();
            SoftDependencies.UnionWith(allSoftReferences);

            if (Auxide.verbose) Utils.DoLog("Running ScriptLoading");
            Manager.ScriptLoading(this);
            Utils.DoLog("Ran ScriptLoading");

            try
            {
                if (Auxide.verbose) Utils.DoLog("Initializing Instance");
                Instance.Initialize();
                if (Auxide.verbose) Utils.DoLog("Initialized Instance");
            }
            catch (Exception e)
            {
                if (Auxide.verbose) Utils.DoLog("Initialize error");
                ReportError("Initialize", e);
            }

            Debug.LogWarning($"Script {path} loaded!");
            Manager.ScriptLoaded(this);

            //config.Load();
            //data.GetFile(Name).Load();
            //config.Save();
        }

        internal void ReportError(string context, Exception e)
        {
            Debug.LogError($"Script '{Name}' threw in {context}: {e}");
        }

        #region IEquatable
        public bool Equals(Script other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Manager, other.Manager) && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Script)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Manager?.GetHashCode() ?? 0) * 397) ^ (Name?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(Script left, Script right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Script left, Script right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}
