using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        public string SourceCode { get; private set; }
        public Assembly Assembly { get; private set; }
        public RustScript Instance { get; private set; }
        public HashSet<string> SoftDependencies { get; }
        public DynamicConfigFile config { get; private set; }
        public DataFileSystem data { get; private set; }

        public string compileProc;
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
        }

        internal void Update(string path = null)
        {
            if (path == null) return;
            string code;

            try
            {
                Utils.DoLog($"Trying to load code from {path}");
                code = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Utils.DoLog($"Failed to load script code at path: {path}: {e}");
                throw new ScriptLoadException(Name, $"Failed to load script code at path: {path}", e);
            }

            if (code != SourceCode)
            {
                if (!Auxide.useInternal)
                {
                    GetCompiler();
                }
                Compile(path, code);
            }
        }

        private void GetCompiler()
        {
            if (Auxide.useInternal) return;

            string proc = "csc.exe";
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    proc = "mcs";
                    break;
            }
            compileProc = System.IO.Path.Combine(Auxide.BinPath, proc);
            using (WebClient client = new WebClient())
            {
                client.DownloadFile($"https://code.remod.org/{proc}", compileProc);
            }
        }

        private void Compile(string path, string code)
        {
            Utils.DoLog($"Compiling {Name} @ {path}");

            CompilationResult result = null;
            if (Auxide.useInternal)
            {
                result = CSharpCompiler.Build(Name, code, compileProc, path);
            }
            else
            {
                result = CSharpCompilerExternal.Build(Name, code, compileProc, path);
            }

            if (!result.IsSuccess)
            {
                string errorList = string.Join("\n", result.Errors);
                Utils.DoLog($"Script compile failed:\n{errorList}");
                throw new ScriptLoadException(Name, $"Script compile failed:\n{errorList}");
            }

            Assembly assembly;

            try
            {
                assembly = Assembly.Load(result.AssemblyData);
                Utils.DoLog($"Loaded assembly!");
            }
            catch (Exception e)
            {
                Utils.DoLog($"Failed to load assembly: {e}");
                throw new ScriptLoadException(Name, "Failed to load assembly", e);
            }

            Utils.DoLog($"Initializing {path}");
            Initialize(path, code, assembly);
        }

        private void Initialize(string path, string code, Assembly assembly)
        {
            Dispose();

            Type type = assembly.GetType(Name);
            if (type == null)
            {
                Utils.DoLog($"Unable to find class '{Name}' in the compiled script.");
                throw new ScriptLoadException(Name, $"Unable to find class '{Name}' in the compiled script.");
            }

            object instance;

            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Utils.DoLog($"Exception thrown in script '{Name}' constructor.");
                throw new ScriptLoadException(Name, $"Exception thrown in script '{Name}' constructor.", e);
            }

            if (!(instance is RustScript scriptInstance))
            {
                Utils.DoLog($"Script class ({type.FullName}) must derive from RustScript.");
                throw new ScriptLoadException(Name, $"Script class ({type.FullName}) must derive from RustScript.");
            }

            scriptInstance.Manager = Manager;

            Assembly = assembly;
            Instance = scriptInstance;
            Path = path;
            SourceCode = code;

            string BaseName = Name.Replace(".cs", "");
            ConfigPath = System.IO.Path.Combine(Auxide.ConfigPath, $"{BaseName}.json");
            DataPath = System.IO.Path.Combine(Auxide.DataPath, BaseName);

            (instance as RustScript).config = new DynamicConfigFile(ConfigPath);
            (instance as RustScript).data = new DataFileSystem(DataPath);

            SoftDependencies.Clear();
            List<string> allSoftReferences = Manager.PopulateScriptReferences(Instance).ToList();
            SoftDependencies.UnionWith(allSoftReferences);

            Manager.ScriptLoading(this);

            try
            {
                Instance.Initialize();
            }
            catch (Exception e)
            {
                Utils.DoLog("Initialize error");
                ReportError("Initialize", e);
            }

            Debug.LogWarning($"Script {path} loaded!");
            Manager.ScriptLoaded(this);

            config.Load();
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
