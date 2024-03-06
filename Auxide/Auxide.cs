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
        public static ScriptManager Scripts { get; set; }
        public static string BinPath { get; internal set; }
        public static string TopPath { get; internal set; }
        public static string ScriptPath { get; internal set; }
        public static string ConfigPath { get; internal set; }
        public static string DataPath { get; internal set; }
        public static string LangPath { get; internal set; }
        public static string LogPath { get; internal set; }
        public static string LogFile { get; internal set; }

        private static string configFile;
        public static AuxideConfig config;
        public static Permissions permissions;
        public static Interface Interface;

        private static bool initialized;
        public static bool full;
        public static bool verbose;
        private static object nextTickLock;

        private static ActionQueue queue;
        private static Action<float> onFrame;

        public static bool hideGiveNotices { get; internal set; }
        //public static bool useInternal = false;

        static Auxide()
        {
            AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Version = new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);
            if (!initialized) Init();
        }

        public static void Init()
        {
            if (initialized) return;
            Interface = new Interface();

            nextTickLock = new object();
            //CosturaUtility.Initialize();
            try
            {
                string now = DateTime.Now.ToShortTimeString();
                configFile = Path.Combine(AppContext.BaseDirectory, "HarmonyMods", "Auxide.json");
                UnityEngine.Debug.LogWarning($"[Auxide ({now})] Opening config file: {configFile}");
                LoadConfig();

                UnityEngine.Debug.LogWarning(full ? $"[Auxide ({now})] Operating in full plugin mode..." : $"[Auxide ({now})] Operating in minimal, non-plugin mode...");
                TopPath = Path.Combine(AppContext.BaseDirectory, "auxide");
                BinPath = Path.Combine(AppContext.BaseDirectory, "auxide", "bin");
                ScriptPath = Path.Combine(AppContext.BaseDirectory, "auxide", "scripts");
                ConfigPath = Path.Combine(AppContext.BaseDirectory, "auxide", "config");
                DataPath = Path.Combine(AppContext.BaseDirectory, "auxide", "data");
                LangPath = Path.Combine(AppContext.BaseDirectory, "auxide", "lang");
                LogPath = Path.Combine(AppContext.BaseDirectory, "auxide", "logs");

                if (verbose)
                {
                    UnityEngine.Debug.LogWarning($"[Auxide ({now})] BinPath: {BinPath}");
                    UnityEngine.Debug.LogWarning($"[Auxide ({now})] ScriptPath: {ScriptPath}");
                    UnityEngine.Debug.LogWarning($"[Auxide ({now})] ConfigPath: {ConfigPath}");
                    UnityEngine.Debug.LogWarning($"[Auxide ({now})] DataPath: {DataPath}");
                    UnityEngine.Debug.LogWarning($"[Auxide ({now})] LangPath: {LangPath}");
                    UnityEngine.Debug.LogWarning($"[Auxide ({now})] LogPath: {LogPath}");
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
                if (!Directory.Exists(LangPath))
                {
                    Directory.CreateDirectory(LangPath);
                }
                if (!Directory.Exists(LogPath))
                {
                    Directory.CreateDirectory(LogPath);
                }

                Utils.GetNewLog();

                Scripts = new ScriptManager(ScriptPath);
                queue = new ActionQueue();
                permissions = new Permissions();
                initialized = true;
                Interface = new Interface();
                if (verbose) Utils.DoLog(full ? "Initialized full mode with plugins..." : "Initialized minimal mode with no plugins...", false);
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
            //useInternal = config.Options.useInternalCompiler;
            hideGiveNotices = config.Options.hideGiveNotices;

            if (verbose)
            {
                string now = DateTime.Now.ToShortTimeString();
                UnityEngine.Debug.LogWarning($"[Auxide ({now})] Re-read config");
            }
        }

        public object CallHook(string hookname, params object[] args)
        {
            if (!initialized) return null;
            return Scripts?.CallHook(hookname, args);
        }

        public static void NextTick(Action callback)
        {
            lock (nextTickLock)
            {
                queue.Enqueue(callback);
            }
        }

        public static void OnFrame(Action<float> callback)
        {
            onFrame += callback;
        }

        public static void OnFrame(float delta = 0)
        {
            queue.Consume(delta);
        }

        public static void Dispose()
        {
            initialized = false;
        }
    }
}
