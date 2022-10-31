using Auxide.Scripting;
using System;
using System.IO;
using System.Reflection;

namespace Auxide
{
    public class Auxide
    {
        internal readonly static Version AssemblyVersion;

        public readonly static VersionNumber Version;
        public static ScriptManager Scripts { get; internal set; }
        public static string BinPath { get; internal set; }
        public static string TopPath { get; internal set; }
        public static string ScriptPath { get; internal set; }
        public static string ConfigPath { get; internal set; }
        public static string DataPath { get; internal set; }
        public static string LogPath { get; internal set; }
        public static string LogFile { get; internal set; }

        private static string configFile;
        public static AuxideConfig config;

        private static bool initialized;
        public static bool full;
        public static bool verbose;
        public static bool useInternal = false;

        static Auxide()
        {
            AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Version = new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);
            if (!initialized) Init();
        }

        public static void Init()
        {
            if (initialized) return;

            try
            {
                configFile = Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide.json");
                UnityEngine.Debug.LogWarning($"Opening config file: {configFile}");
                LoadConfig();

                UnityEngine.Debug.LogWarning(full ? "Operating in full mode with plugins..." : "Operating in minimal mode with no plugins...");
                TopPath = Path.Combine(AppContext.BaseDirectory, "auxide");
                BinPath = Path.Combine(AppContext.BaseDirectory, "auxide", "Bin");
                ScriptPath = Path.Combine(AppContext.BaseDirectory, "auxide", "Scripts");
                ConfigPath = Path.Combine(AppContext.BaseDirectory, "auxide", "Config");
                DataPath = Path.Combine(AppContext.BaseDirectory, "auxide", "Data");
                LogPath = Path.Combine(AppContext.BaseDirectory, "auxide", "Logs");
                LogFile = Path.Combine(LogPath, "auxide.log");

                if (verbose)
                {
                    UnityEngine.Debug.LogWarning($"BinPath: {BinPath}");
                    UnityEngine.Debug.LogWarning($"ScriptPath: {ScriptPath}");
                    UnityEngine.Debug.LogWarning($"ConfigPath: {ConfigPath}");
                    UnityEngine.Debug.LogWarning($"DataPath: {DataPath}");
                    UnityEngine.Debug.LogWarning($"LogPath: {LogPath}");
                }

                if (!Directory.Exists(BinPath))
                {
                    Directory.CreateDirectory(BinPath);
                }
                if (!Directory.Exists(ScriptPath))
                {
                    Directory.CreateDirectory(ScriptPath);
                }
                if (!Directory.Exists(ConfigPath))
                {
                    Directory.CreateDirectory(ConfigPath);
                }
                if (!Directory.Exists(DataPath))
                {
                    Directory.CreateDirectory(DataPath);
                }
                if (!Directory.Exists(LogPath))
                {
                    Directory.CreateDirectory(LogPath);
                }

                Scripts = new ScriptManager(ScriptPath);//, ConfigPath, DataPath);
                initialized = true;
                if (verbose) Utils.DoLog("Initialized!", false);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        public static void LoadConfig()
        {
            if (!File.Exists(configFile))
            {
                config = new AuxideConfig(configFile);
                config.Save();
            }
            else
            {
                config = ConfigFile.Load<AuxideConfig>(configFile);
            }

            full = config.Options.full;
            verbose = config.Options.verbose;
            useInternal = config.Options.useInternalCompiler;
        }

        public static void Dispose()
        {
            initialized = false;
        }
        //public static string AssemblyDirectory()
        //{
        //    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        //    UriBuilder uri = new UriBuilder(codeBase);
        //    string path = Uri.UnescapeDataString(uri.Path);
        //    return Path.GetDirectoryName(path);
        //}
    }
}
