using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Auxide
{
    public class ScriptManager : IDisposable
    {
        //private const string ScriptExtension = ".cs";
        private const string ScriptExtension = ".dll";
        //private const string DLLExtension = ".dll";
        private string ScriptFilter = "*" + ScriptExtension;
        private const double UpdateFrequency = 1.0 / 1; // per sec
        private const double ChangeCooldown = 1; // seconds
        private static readonly char[] NameTrimChars = { '_' };

        private readonly object _sync;
        private readonly string _sourcePath;
        private readonly Dictionary<string, Script> _scripts;
        private readonly FileSystemWatcher _watcher;
        private readonly HashSet<RefreshItem> _pendingRefresh;
        private readonly Stopwatch _timeSinceChange;
        private readonly Stopwatch _timeSinceUpdate;

        /// <summary>
        /// Called after instantiating the script but before Initialize is called. Use this to set up the instance with auto-populated field values.
        /// </summary>
        public event Action<IScriptReference> OnScriptLoading;

        /// <summary>
        /// Called after Initialize finishes for a script.
        /// </summary>
        public event Action<IScriptReference> OnScriptLoaded;

        /// <summary>
        /// Called before Dispose is called for a script.
        /// </summary>
        public event Action<IScriptReference> OnScriptUnloading;

        /// <summary>
        /// Called after Dispose finishes for a script.
        /// </summary>
        public event Action<IScriptReference> OnScriptUnloaded;

        // This was added for the compilers as a test.  Possibly can or should remove.
        public ScriptManager()
        {
            _sync = new object();
            _scripts = Auxide.Scripts._scripts;
        }

        public ScriptManager(string sourcePath)//, string configPath, string dataPath)
        {
            //if (!Auxide.config.Options.cSharpScripts)
            //{
            //    ScriptFilter = "*" + DLLExtension;
            //}

            _sync = new object();
            _sourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            //_configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
            //_dataPath   = dataPath   ?? throw new ArgumentNullException(nameof(dataPath));
            _scripts = new Dictionary<string, Script>(StringComparer.OrdinalIgnoreCase);
            _pendingRefresh = new HashSet<RefreshItem>();
            _timeSinceChange = Stopwatch.StartNew();
            _timeSinceUpdate = Stopwatch.StartNew();

            RefreshAll();

            _watcher = new FileSystemWatcher(sourcePath, ScriptFilter)
            {
                InternalBufferSize = 32 * 1024,
                EnableRaisingEvents = true,
            };

            _watcher.Created += (sender, args) => Refresh(args.FullPath);
            _watcher.Deleted += (sender, args) => Refresh(args.FullPath);
            _watcher.Changed += (sender, args) => Refresh(args.FullPath);
            _watcher.Renamed += (sender, args) =>
            {
                Refresh(args.FullPath);
                Refresh(args.OldFullPath);
            };
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        internal void Update()
        {
            if (_timeSinceUpdate.Elapsed.TotalSeconds < UpdateFrequency)
            {
                return;
            }

            _timeSinceUpdate.Restart();

            lock (_sync)
            {
                if (_timeSinceChange.Elapsed.TotalSeconds < ChangeCooldown)
                {
                    return;
                }

                if (_pendingRefresh.Count == 0)
                {
                    return;
                }

                // TODO: this will be a lot more involved once it supports dependencies between scripts
                foreach (RefreshItem item in _pendingRefresh)
                {
                    if (!File.Exists(item.Path))
                    {
                        if (_scripts.TryGetValue(item.Name, out Script script))
                        {
                            script.Dispose();
                            _scripts.Remove(item.Name);
                        }
                    }
                    else if (_scripts.TryGetValue(item.Name, out Script script))
                    {
                        UpdateScript(script, item.Path);
                    }
                    else
                    {
                        Script newScript = new Script(this, item.Name);
                        _scripts.Add(item.Name, newScript);

                        UpdateScript(newScript, item.Path);
                    }
                }

                _pendingRefresh.Clear();
            }
        }

        private void UpdateScript(Script script, string path)
        {
            try
            {
                script.Update(path);
            }
            catch (Exception e)
            {
                script.ReportError("Update", e);
            }
        }

        private void Refresh(string scriptPath)
        {
            string name = Path.GetFileNameWithoutExtension(scriptPath);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            lock (_sync)
            {
                _pendingRefresh.Add(new RefreshItem(name, scriptPath));
                _timeSinceChange.Restart();
            }
        }

        private void RefreshAll()
        {
            foreach (string scriptPath in Directory.EnumerateFiles(_sourcePath, ScriptFilter))
            {
                Refresh(scriptPath);
            }
        }

        internal void ScriptLoading(IScriptReference script)
        {
            OnScriptLoading?.Invoke(script);
        }

        internal void ScriptLoaded(IScriptReference script)
        {
            OnScriptLoaded?.Invoke(script);
            Broadcast("OnScriptLoaded", script);
            Broadcast("LoadData", script);
            Broadcast("LoadConfig", script);
        }

        internal void ScriptUnloading(IScriptReference script)
        {
            OnScriptUnloading?.Invoke(script);
        }

        internal void ScriptUnloaded(IScriptReference script)
        {
            OnScriptUnloaded?.Invoke(script);
            Broadcast("OnScriptUnloaded", script);
        }

        #region Standard Hooks
        public void OnServerInitializedHook()
        {
            Broadcast("OnServerInitialized");
        }

        public void OnServerShutdownHook()
        {
            Broadcast("OnServerShutdown");
        }

        public void OnServerSaveHook()
        {
            Broadcast("OnServerSave");
        }

        public void OnNewSaveHook()
        {
            Broadcast("OnNewSave");
        }

        public void OnGroupCreatedHook(string group, string title, int rank)
        {
            Broadcast("OnGroupCreated", group, title, rank);
        }

        public void OnUserGroupAddedHook(string id, string name)
        {
            Broadcast("OnUserGroupAdded", id, name);
        }

        public object CanUseUIHook(BasePlayer player, string json)
        {
            return BroadcastReturn("CanUseUI", player, json);
        }

        public void OnDestroyUIHook(BasePlayer player, string elem)
        {
            Broadcast("OnDestroyUI", player, elem);
        }

        public object CanAdminTCHook(BuildingPrivlidge bp, BasePlayer player)
        {
            if (bp == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanAdminTC", bp, player);
        }

        public object CanToggleSwitchHook(BaseOven oven, BasePlayer player)
        {
            if (oven == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanToggleSwitch", oven, player);
        }

        public object CanToggleSwitchHook(ElectricSwitch sw, BasePlayer player)
        {
            if (sw == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanToggleSwitch", sw, player);
        }

        public void OnToggleSwitchHook(BaseOven oven, BasePlayer player)
        {
            if (oven == null) return;
            if (player == null) return;
            Broadcast("OnToggleSwitch", oven, player);
        }

        public void OnToggleSwitchHook(ElectricSwitch sw, BasePlayer player)
        {
            if (sw == null) return;
            if (player == null) return;
            Broadcast("OnToggleSwitch", sw, player);
        }

        public object CanMountHook(BaseMountable entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanMount", entity, player);
        }

        public void OnMountedHook(BaseMountable entity = null, BasePlayer player = null)
        {
            if (entity == null) return;
            if (player == null) return;
            Broadcast("OnMounted", entity, player);
        }

        public object CanLootHook(BaseEntity entity = null, BasePlayer player = null, string panelName = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanLoot", entity, player, panelName);
        }

        public void OnLootedHook(BaseEntity entity = null, BasePlayer player = null)
        {
            Broadcast("OnLooted", entity, player);
        }

        public object CanPickupHook(ContainerIOEntity entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanPickup", entity, player);
        }

        public object CanPickupHook(StorageContainer entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanPickup", entity, player);
        }

        public object CanPickupHook(BaseCombatEntity entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanPickup", entity, player);
        }

        public object OnTakeDamageHook(BaseCombatEntity target = null, HitInfo info = null)
        {
            return BroadcastReturn("OnTakeDamage", target, info);
        }

        public void OnPlayerJoinHook(BasePlayer player = null)
        {
            if (player == null) return;
            Broadcast("OnPlayerJoin", player);
        }

        public void OnPlayerLeaveHook(BasePlayer player = null)
        {
            if (player == null) return;
            Broadcast("OnPlayerLeave", player);
        }

        public object OnConsoleCommandHook(string command, bool isServer = false)
        {
            if (Auxide.full)
            {
                return BroadcastReturn("OnConsoleCommand", command, isServer);
            }
            //OnChatCommandHook(null, command);
            return null;
        }

        internal void OnChatCommandHook(BasePlayer player, string chat, object[] args = null)
        {
            Match m = Regex.Match(chat, @"^/a\.", RegexOptions.IgnoreCase);
            Match m2 = Regex.Match(chat, @"^/auxide\.", RegexOptions.IgnoreCase);
            string[] hookArgs = chat.Split(' ');
            string command = hookArgs[0].Replace("/", "");
            if (player.IsAdmin && (m.Success || m2.Success))
            {
                if (command.Equals("a.version") || command.Equals("auxide.version"))
                {
                    Assembly assem = Assembly.GetExecutingAssembly();
                    AssemblyName assemName = assem.GetName();
                    Version ver = assemName.Version;
                    player.ChatMessage($"{assemName} {ver}");
                    return;
                }
                else if (command.Equals("a.verbose") || command.Equals("auxide.verbose"))
                {
                    Auxide.verbose = !Auxide.verbose;
                    player.ChatMessage($"Verbose is now {Auxide.verbose}");
                    return;
                }
                else if (command.Equals("a.unload") || command.Equals("auxide.unload"))
                {
                    string[] chatElements = chat.Split(' ');
                    if (chatElements.Length == 2)
                    {
                        if (_scripts.TryGetValue(chatElements[1], out Script script))
                        {
                            script.Dispose();
                            _scripts.Remove(chatElements[1]);
                        }
                    }
                }
                else if (command.Equals("a.reload") || command.Equals("auxide.reload"))
                {
                    if (hookArgs.Length == 2)
                    {
                        string scriptName = hookArgs[1].Replace(".dll", "");
                        if (_scripts.TryGetValue(scriptName, out Script script))
                        {
                            script.Dispose();
                            _scripts.Remove(scriptName);

                            Script newScript = new Script(this, Path.Combine(Auxide.ScriptPath, $"{scriptName}.dll"));
                            _scripts.Add(scriptName, newScript);
                            UpdateScript(newScript, scriptName);
                        }
                    }
                }
                else if (command.Equals("a.load") || command.Equals("auxide.load"))
                {
                    if (hookArgs.Length == 2)
                    {
                        string scriptName = hookArgs[1].Replace(".dll", "");
                        if (!_scripts.TryGetValue(scriptName, out _))
                        {
                            Script newScript = new Script(this, Path.Combine(Auxide.ScriptPath, $"{scriptName}.dll"));
                            _scripts.Add(scriptName, newScript);
                            UpdateScript(newScript, scriptName);
                        }
                    }
                }
                else if (command.Equals("a.info") || command.Equals("auxide.info"))
                {
                    string verbose = Auxide.verbose.ToString();
                    //string useint = Auxide.useInternal.ToString();
                    string runMode = Auxide.full ? "full" : "minimal";
                    Assembly assem = Assembly.GetExecutingAssembly();
                    AssemblyName assemName = assem.GetName();
                    Version ver = assemName.Version;
                    string message = $"{assemName} {ver}\nRun Mode: {runMode}\nVerboseLogging: {verbose}";
                    player.ChatMessage(message);
                    return;
                }
                else if (command.Equals("a.list") || command.Equals("auxide.list"))
                {
                    string message = "";
                    foreach(KeyValuePair<string, Script> script in _scripts)
                    {
                        message += $"{script.Key}, {script.Value.Instance.Version}\n";
                    }
                    player.ChatMessage(message);
                    return;
                }
            }
            if (Auxide.full) Broadcast("OnChatCommand", player, command, hookArgs);
        }
        #endregion

        internal IEnumerable<string> PopulateScriptReferences(RustScript rustScript)
        {
            Type type = rustScript.GetType();

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(IScriptReference) || field.IsSpecialName)
                {
                    continue;
                }

                string scriptName = field.Name.Trim(NameTrimChars);
                if (!_scripts.TryGetValue(scriptName, out Script script))
                {
                    continue;
                }

                field.SetValue(rustScript, script);
                yield return script.Name;
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType != typeof(IScriptReference) || !property.CanWrite)
                {
                    continue;
                }

                string scriptName = property.Name.Trim(NameTrimChars);
                if (!_scripts.TryGetValue(scriptName, out Script script))
                {
                    continue;
                }

                property.SetValue(rustScript, script);
                yield return script.Name;
            }
        }

        public void Broadcast(string methodName)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                    script.InvokeProcedure(methodName);
                }
            }
        }

        public void Broadcast<T0>(string methodName, T0 arg0)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                    script.InvokeProcedure(methodName, arg0);
                }
            }
        }

        public void Broadcast<T0, T1>(string methodName, T0 arg0, T1 arg1)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                   script.InvokeProcedure(methodName, arg0, arg1);
                }
            }
        }

        public void Broadcast<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                   script.InvokeProcedure(methodName, arg0, arg1, arg2);
                }
            }
        }

        public void Broadcast<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                   script.InvokeProcedure(methodName, arg0, arg1, arg2, arg3);
                }
            }
        }

        public void Broadcast<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                   script.InvokeProcedure(methodName, arg0, arg1, arg2, arg3, arg4);
                }
            }
        }

        public object BroadcastReturn(string methodName)
        {
            //object[] returns = new object[_scripts.Count];
            object rtrn = null;
            object lastRet = null;
            string lastScript = "";

            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                    rtrn = script.InvokeFunction<object>(methodName);
                    if (lastRet != null && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script.Name;
                }
                rtrn = lastRet;
            }

            return rtrn;
        }

        public object BroadcastReturn<T0>(string methodName, T0 arg0)
        {
            object rtrn = null;
            object lastRet = null;
            string lastScript = "";

            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                    rtrn = script.InvokeFunction<object, T0>(methodName, arg0);
                    if (lastRet != null && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script.Name;
                }
                rtrn = lastRet;
            }

            return rtrn;
        }

        public object BroadcastReturn<T0, T1>(string methodName, T0 arg0, T1 arg1)
        {
            object rtrn = null;
            object lastRet = null;
            string lastScript = "";

            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                    rtrn = (object) script.InvokeFunction<T0, T1, object>(methodName, arg0, arg1);
                    if (lastRet != null && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script.Name;
                }
                rtrn = lastRet;
            }

            return rtrn;
        }

        public object BroadcastReturn<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2)
        {
            object rtrn = null;
            object lastRet = null;
            string lastScript = "";

            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                    rtrn = script.InvokeFunction<T0, T1, T2, object>(methodName, arg0, arg1, arg2);
                    if (lastRet != null && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script.Name;
                }
                rtrn = lastRet;
            }

            return rtrn;
        }

        public object BroadcastReturn<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            object rtrn = null;
            object lastRet = null;
            string lastScript = "";

            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
                {
                    rtrn = script.InvokeFunction<T0, T1, T2, T3, object>(methodName, arg0, arg1, arg2, arg3);
                    if (lastRet != null && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script.Name;
                }
                rtrn = lastRet;
            }

            return rtrn;
        }

        //public object BroadcastReturn<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        //{
        //    object rtrn = null;
        //    object lastRet = null;
        //    string lastScript = "";

        //    lock (_sync)
        //    {
        //        foreach (Script script in _scripts.Values.Where(x => x.initialized == true))
        //        {
        //            rtrn = script.InvokeFunction<T0, T1, T2, T3, T4, object>(methodName, arg0, arg1, arg2, arg3, arg4);
        //            if (lastRet != null && !rtrn.Equals(lastRet))
        //            {
        //                Utils.DoLog($"Conflict between {lastScript} and {script.Name} return values!");
        //            }
        //            lastRet = rtrn;
        //            lastScript = script.Name;
        //        }
        //        rtrn = lastRet;
        //    }

        //    return rtrn;
        //}
    }
}
