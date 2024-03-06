using Auxide.Exceptions;
using Auxide.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Auxide
{
    internal partial class Script : IEquatable<Script>, IDisposable
    {
        public CommandAttribute CommandAttribute { get; private set; }
        public ScriptManager Manager { get; }
        public string Name { get; }
        public bool remote { get; set; }
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
                Debug.LogWarning($"Script {Instance?.GetType().Name} unloaded...");
                DoLog($"Script {Instance?.GetType().Name} unloaded...");
                Instance?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"{Instance?.GetType().Name}::Dispose threw: {e}");
            }

            Instance = null;

            Assembly = null;

            Manager.ScriptUnloaded(this);
        }

        internal void Update(byte[] data = null)
        {
            if (data == null) return;
            const string code = "";
            try
            {
                DoLog("Trying to load dll from remote data");
                Assembly assembly = Assembly.Load(data);
                DoLog($"Loaded assembly: {assembly.GetType()}!");

                Initialize(null, code, assembly);
                initialized = true;
            }
            catch (Exception e)
            {
                initialized = false;
                DoLog($"Failed to load dll from remote data: {e}");
                throw new ScriptLoadException(Name, "Failed to load dll from remote data", e);
            }
        }

        internal void Update(string path = null)
        {
            if (path == null) return;
            const string code = "";

            //if (Auxide.config.Options.cSharpScripts)
            //{
            //    try
            //    {
            //        DoLog($"Trying to load code from {path}");
            //        code = File.ReadAllText(path);
            //    }
            //    catch (Exception e)
            //    {
            //        DoLog($"Failed to load script code at path: {path}: {e}");
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
                DoLog($"Trying to load dll from {path}");
                //Assembly assembly = Assembly.LoadFile(path);
                //Assembly assembly = Assembly.Load(File.ReadAllBytes(path));
                Assembly assembly = Assembly.Load(ReadAllBytes(path));
                DoLog($"Loaded assembly: {assembly.GetType()}!");

                Initialize(path, code, assembly);
                initialized = true;
            }
            catch (Exception e)
            {
                initialized = false;
                DoLog($"Failed to load dll at path: {path}: {e}");
                throw new ScriptLoadException(Name, $"Failed to load dll at path: {path}", e);
            }
            //}
        }

        public byte[] ReadAllBytes(string fileName)
        {
            byte[] buffer = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                buffer = new byte[fs.Length];
                _ = fs.Read(buffer, 0, checked((int)fs.Length));
            }
            return buffer;
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
        //    DoLog($"Compiling {Name} @ {path}");

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
        //        DoLog($"Script compile failed:\n{errorList}");
        //        throw new ScriptLoadException(Name, $"Script compile failed:\n{errorList}");
        //    }

        //    Assembly assembly;

        //    try
        //    {
        //        assembly = Assembly.Load(result.AssemblyData);
        //        DoLog($"Loaded assembly!");
        //    }
        //    catch (Exception e)
        //    {
        //        DoLog($"Failed to load assembly: {e}");
        //        throw new ScriptLoadException(Name, "Failed to load assembly", e);
        //    }

        //    DoLog($"Initializing {path}");
        //    Initialize(path, code, assembly);
        //}

        private void DoLog(string text) => Utils.DoLog(text, false, false);

        private void Initialize(string path, string code, Assembly assembly)
        {
            Dispose(); // Hrm...
            DoLog("Initializing assembly");

            Type type = assembly.GetType(Name);

            if (type == null)
            {
                DoLog($"Unable to find class '{Name}' in the compiled script.");
                throw new ScriptLoadException(Name, $"Unable to find class '{Name}' in the compiled script.");
            }

            object instance;

            try
            {
                DoLog($"Attempting to create instance of type {type}");
                instance = Activator.CreateInstance(type);// as RustScript;
                DoLog($"Created instance of type {instance.GetType().FullName}");
            }
            catch (Exception e)
            {
                DoLog($"Exception thrown in script '{Name}' constructor.");
                throw new ScriptLoadException(Name, $"Exception thrown in script '{Name}' constructor.", e);
            }

            if (!(instance is RustScript scriptInstance))
            {
                DoLog($"Script class ({instance.GetType()}:{instance.GetType().BaseType}) must derive from RustScript.");
                throw new ScriptLoadException(Name, $"Script class ({type.FullName}) must derive from RustScript.");
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
            LangPath = Auxide.LangPath;

            scriptInstance.config = new DynamicConfigFile(ConfigPath);
            scriptInstance.data = new DataFileSystem(DataPath);
            scriptInstance.lang = new LangFileSystem(LangPath);

            SoftDependencies.Clear();
            List<string> allSoftReferences = Manager.PopulateScriptReferences(Instance).ToList();
            SoftDependencies.UnionWith(allSoftReferences);

            if (Auxide.verbose) DoLog("Running ScriptLoading");
            Manager.ScriptLoading(this);
            DoLog("Ran ScriptLoading");

            try
            {
                if (Auxide.verbose) DoLog("Initializing Instance");
                Instance.Initialize();
                if (Auxide.verbose) DoLog("Initialized Instance");
            }
            catch (Exception e)
            {
                if (Auxide.verbose) DoLog("Initialize error");
                ReportError("Initialize", e);
            }

            Debug.LogWarning($"Script {path} loaded!");
            Manager.ScriptLoaded(this);
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
