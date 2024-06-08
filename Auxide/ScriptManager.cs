using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Auxide
{
    public class ScriptManager : IDisposable
    {
        private const string ScriptExtension = ".cs";
        //private const string ScriptExtension = ".dll";
        //private const string DLLExtension = ".dll";
        private string ScriptFilter = "*" + ScriptExtension;
        private const double UpdateFrequency = 2;// / 1; // per sec
        private const double ChangeCooldown = 2; // seconds
        private static readonly char[] NameTrimChars = { '_' };

        private readonly object _sync;
        private readonly string _sourcePath;
        private readonly Dictionary<string, Script> _scripts;
        private readonly FileSystemWatcher _watcher;
        private readonly HashSet<RefreshItem> _pendingRefresh;
        private readonly Stopwatch _timeSinceChange;
        private readonly Stopwatch _timeSinceUpdate;

        internal bool playerTakingDamage;
        internal bool serverInitialized;

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

        //private readonly SubscriptionClient Client;

        // This was added for the compilers as a test.  Possibly can or should remove.
        public ScriptManager()
        {
            _sync = new object();
            _scripts = Auxide.Scripts._scripts;
        }

        public ScriptManager(string sourcePath)
        {
            _sync = new object();
            _sourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            _scripts = new Dictionary<string, Script>(StringComparer.OrdinalIgnoreCase);
            _pendingRefresh = new HashSet<RefreshItem>();
            _timeSinceChange = Stopwatch.StartNew();
            _timeSinceUpdate = Stopwatch.StartNew();

            //if (Auxide.config.Options.subscription.enabled)
            //{
            //    Client = new SubscriptionClient();
            //    foreach (SubscriptionClient.AuxideSubscribedPlugin pluginsub in Client.current.data)
            //    {
            //        try
            //        {
            //            Script newScript = new Script(this, pluginsub.name)
            //            {
            //                remote = true
            //            };
            //            _scripts.Add(pluginsub.name, newScript);
            //            newScript.Update(pluginsub.data);
            //        }
            //        catch (Exception e)
            //        {
            //            Utils.DoLog($"Unable to process subscribed plugin: {e}");
            //        }
            //    }
            //}

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

        public object GetAll()
        {
            return _scripts.Values;
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

        public void UnloadAll()
        {
            foreach (Script script in _scripts.Values)
            {
                OnScriptUnloading?.Invoke(script);
                script.Dispose();
            }
        }

        public object CallHook(string hook, params object[] args)
        {
            //if (Auxide.verbose) Utils.DoLog($"CallHook called for {hook} with {args.Length} args");
            if (!serverInitialized) return null;
            object[] objArray = Utils.ArrayPool.Get(args.Length);
            int i = 0;
            foreach (object obj in args)
            {
                objArray[i] = obj;
                i++;
            }

            switch (objArray.Length)
            {
                case 4:
                    {
                        object res = BroadcastReturn(hook, objArray[0], objArray[1], objArray[2], objArray[3]);
                        Utils.ArrayPool.Free(objArray);
                        return res;
                    }
                case 3:
                    {
                        object res = BroadcastReturn(hook, objArray[0], objArray[1], objArray[2]);
                        Utils.ArrayPool.Free(objArray);
                        return res;
                    }
                case 2:
                    {
                        object res = BroadcastReturn(hook, objArray[0], objArray[1]);
                        Utils.ArrayPool.Free(objArray);
                        return res;
                    }
                case 1:
                    {
                        object res = BroadcastReturn(hook, objArray[0]);
                        Utils.ArrayPool.Free(objArray);
                        return res;
                    }
                default:
                    {
                        object res = BroadcastReturn(hook);
                        Utils.ArrayPool.Free(objArray);
                        return res;
                    }
            }
        }

        //public static object CallHook(string hook, object obj1, object obj2)
        //{
        //    if (Auxide.verbose) Utils.DoLog($"CallHook called for {hook}");
        //    //object[] objArray = Utils.ArrayPool.Get(2);
        //    //objArray[0] = obj1;
        //    //objArray[1] = obj2;
        //    //object result = Auxide.Scripts.BroadcastReturn(hook, objArray);
        //    //object result = Auxide.Scripts.BroadcastReturn(hook, obj1, obj2);
        //    ScriptManager obj = new ScriptManager();
        //	return obj.BroadcastReturn(hook, obj1, obj2);
        //	//Utils.ArrayPool.Free(objArray);
        //	//return result;
        //}

        internal void ScriptLoading(IScriptReference script)
        {
            OnScriptLoading?.Invoke(script);
        }

        internal void ScriptLoaded(IScriptReference script)
        {
            OnScriptLoaded?.Invoke(script);
            Narrowcast("OnScriptLoaded", script);
            //Narrowcast("LoadData", script);
            //Narrowcast("LoadConfig", script);
            Narrowcast("LoadDefaultMessages", script);
            Broadcast("OnPluginLoaded", script);
        }

        internal void ScriptUnloading(IScriptReference script)
        {
            Narrowcast("Unload", script);
            OnScriptUnloading?.Invoke(script);
        }

        internal void ScriptUnloaded(IScriptReference script)
        {
            OnScriptUnloaded?.Invoke(script);
            Broadcast("OnScriptUnloaded", script);
            Broadcast("OnPluginUnloaded", script);
        }

        #region Standard Hooks
        internal void OnTickHook()
        {
            Broadcast("OnTick");
        }

        internal void OnServerInitializeHook()
        {
            serverInitialized = true;
            Broadcast("OnServerInitialize");
        }

        internal void OnServerInitializedHook()
        {
            serverInitialized = true;
            Broadcast("OnServerInitialized");
        }

        internal void OnServerShutdownHook()
        {
            Broadcast("OnServerShutdown");
        }

        internal void OnServerSaveHook()
        {
            Broadcast("OnServerSave");
        }

        internal void OnNewSaveHook(string strFileName)
        {
            Broadcast("OnNewSave", strFileName);
        }

        internal void OnGroupCreatedHook(string group, string title, int rank)
        {
            Broadcast("OnGroupCreated", group, title, rank);
        }

        internal void OnUserGroupAddedHook(string id, string name)
        {
            Broadcast("OnUserGroupAdded", id, name);
        }

        internal object CanUseUIHook(BasePlayer player, string json)
        {
            return BroadcastReturn("CanUseUI", player, json);
        }

        internal void OnDestroyUIHook(BasePlayer player, string elem)
        {
            Broadcast("OnDestroyUI", player, elem);
        }

        internal object CanAdminTCHook(BuildingPrivlidge bp, BasePlayer player)
        {
            if (bp == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanAdminTC", bp, player);
        }

        internal object CanToggleSwitchHook(BaseOven oven, BasePlayer player)
        {
            if (oven == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanToggleSwitch", oven, player);
        }

        internal object CanToggleSwitchHook(ElectricSwitch sw, BasePlayer player)
        {
            if (sw == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanToggleSwitch", sw, player);
        }

        internal void OnToggleSwitchHook(BaseOven oven, BasePlayer player)
        {
            if (oven == null) return;
            if (player == null) return;
            Broadcast("OnToggleSwitch", oven, player);
        }

        internal void OnToggleSwitchHook(ElectricSwitch sw, BasePlayer player)
        {
            if (sw == null) return;
            if (player == null) return;
            Broadcast("OnToggleSwitch", sw, player);
        }

        internal object CanMountHook(BaseMountable entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanMount", entity, player);
        }

        internal void OnMountedHook(BaseMountable entity = null, BasePlayer player = null)
        {
            if (entity == null) return;
            if (player == null) return;
            Broadcast("OnMounted", entity, player);
        }

        internal object CanAcceptItemHook(ItemContainer container, Item item, int targetPos)
        {
            if (container == null) return null;
            if (item == null) return null;
            return BroadcastReturn("CanAcceptItem", container, item, targetPos);
        }

        internal object CanLootHook(BaseEntity entity = null, BasePlayer player = null, string panelName = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanLoot", entity, player, panelName);
        }

        internal void OnLootedHook(BaseEntity entity = null, BasePlayer player = null)
        {
            Broadcast("OnLooted", entity, player);
        }

        internal object CanPickupHook(ContainerIOEntity entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanPickup", entity, player);
        }

        internal object CanPickupHook(StorageContainer entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanPickup", entity, player);
        }

        internal object CanPickupHook(BaseCombatEntity entity = null, BasePlayer player = null)
        {
            if (entity == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanPickup", entity, player);
        }

        internal void OnPickedUpHook(BaseCombatEntity entity = null, Item item = null, BasePlayer player = null)
        {
            if (entity == null) return;
            if (item == null) return;
            if (player == null) return;
            Broadcast("OnPickedUp", entity, item, player);
        }

        internal object CanPlayerAccessMarketHook(MarketTerminal instance, BasePlayer player)
        {
            if (instance == null) return null;
            if (player == null) return null;
            return BroadcastReturn("CanPlayerAccessMarket", instance, player);
        }

        internal void OnMarketUpdateHook(MarketTerminal instance)
        {
            if (instance == null) return;
            Broadcast("OnMarketUpdate", instance);
        }

        internal void OnMarketOrderStartHook(MarketTerminal instance, ulong customerid)
        {
            if (instance == null) return;
            Broadcast("OnMarketOrderStart", instance, customerid);
        }

        internal void OnMarketOrderFinishHook(MarketTerminal instance, ulong customerid)
        {
            if (instance == null) return;
            Broadcast("OnMarketOrderFinish", instance, customerid);
        }

        //public object OnTakeDamageHook(BasePlayer player = null, HitInfo info = null)
        //{
        //    playerTakingDamage = true;
        //    try
        //    {
        //        return BroadcastReturn("OnTakeDamage", player, info);
        //    }
        //    finally
        //    {
        //        playerTakingDamage = false;
        //    }
        //}

        internal object OnDecayHealHook(DecayEntity target)
        {
            if (target == null) return null;
            if (Auxide.verbose) Utils.DoLog($"OnDecayHealHook for {target?.ShortPrefabName}");
            return BroadcastReturn("OnDecayHeal", target);
        }

        internal object OnDecayDamageHook(DecayEntity target)
        {
            if (target == null) return null;
            if (Auxide.verbose) Utils.DoLog($"OnDecayDamageHook for {target?.ShortPrefabName}");
            return BroadcastReturn("OnDecayDamage", target);
        }

        internal object OnTakeDamageHook(BaseCombatEntity target = null, HitInfo info = null)
        {
            if (target == null) return null;
            if (info == null) return null;
            if (info?.HitEntity == null) return null;

            bool selfdamage = false;
            if (info?.HitEntity == target) selfdamage = true;
            //BasePlayer player = target as BasePlayer;
            //if (player != null)
            //{
            //    if (playerTakingDamage) return null;
            //    playerTakingDamage = true;
            //    try
            //    {
            //        if (Auxide.verbose && !selfdamage) Utils.DoLog($"OnTakeDamageHook for {info?.HitEntity?.ShortPrefabName} attacking BasePlayer");
            //        return BroadcastReturn("OnTakeDamage", player, info);
            //    }
            //    finally
            //    {
            //        playerTakingDamage = false;
            //    }
            //}
            if (Auxide.verbose && !selfdamage) Utils.DoLog($"OnTakeDamageHook for {info?.HitEntity?.ShortPrefabName} attacking {target?.ShortPrefabName}");
            return BroadcastReturn("OnTakeDamage", target, info);
        }

        internal object OnHammerHitHook(BasePlayer ownerPlayer, HitInfo hitInfo)
        {
            if (ownerPlayer == null) return null;
            if (hitInfo?.HitEntity == null) return null;
            return BroadcastReturn("OnHammerHit", ownerPlayer, hitInfo);
        }

        internal object OnServerMessageHook(string message, string username, string color, ulong userid)
        {
            return BroadcastReturn("OnServerMessage", message, username, color, userid);
        }

        internal object OnPlayerTickHook(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
        {
            if (player == null) return null;
            //if (msg == null) return null;
            return BroadcastReturn("OnPlayerTick", player, msg, wasPlayerStalled);
        }

        internal object OnPlayerInputHook(BasePlayer player = null, InputState input = null)
        {
            if (player == null) return null;
            if (input == null) return null;
            return BroadcastReturn("OnPlayerInput", player, input);
        }

        internal void OnEntityDeathHook(BaseCombatEntity entity, HitInfo info = null)
        {
            if (entity == null) return;
            if (info == null) return;
            Broadcast("OnEntityDeath", entity, info);
        }

        internal void OnPlayerJoinHook(BasePlayer player = null)
        {
            if (player == null) return;
            if (player.IsAdmin) Permissions.AddGroupMember("admin", player.UserIDString);
            Permissions.AddGroupMember("default", player.UserIDString);

            Broadcast("OnPlayerJoin", player);
        }

        //public object OnEntitySavedHook(BuildingPrivlidge entity, BaseNetworkable.SaveInfo saveInfo)
        //{
        //    //return OnEntitySavedHook(entity as BaseNetworkable, saveInfo);
        //    if (entity == null) return null;
        //    if (!serverInitialized || saveInfo.forConnection == null) return null;
        //    return BroadcastReturn("OnEntitySaved", entity, saveInfo);
        //}
        //internal object OnEntitySavedHook(object entity, BaseNetworkable.SaveInfo saveInfo)
        //internal object OnEntitySavedHook(BaseNetworkable entity, BaseNetworkable.SaveInfo saveInfo)
        //internal object OnEntitySavedHook(BaseNetworkable entity, BaseNetworkable.SaveInfo saveInfo)
        //{
        //    return OnEntitySavedHook(entity, saveInfo);
        //}

        //internal object OnEntitySavedHook(object entity, BaseNetworkable.SaveInfo saveInfo)
        internal object OnEntitySavedHook(BaseNetworkable entity, BaseNetworkable.SaveInfo saveInfo)
        {
            if (entity == null) return null;
            //if (!(entity is BaseNetworkable)) return null;
            if (!serverInitialized || saveInfo.forConnection == null) return null;
            //return BroadcastReturn("OnEntitySaved", entity as BaseNetworkable, saveInfo);
            //Broadcast("OnEntitySaved", entity as BaseNetworkable, saveInfo);
            Broadcast("OnEntitySaved", entity, saveInfo);
            return null;
        }

        internal void OnPlayerLeaveHook(BasePlayer player = null)
        {
            if (player == null) return;
            Broadcast("OnPlayerLeave", player);
        }

        internal object OnServerCommandHook(ConsoleSystem.Arg arg)
        {
            if (Auxide.full)
            {
                Utils.DoLog("OnServerCommandHook was called");
                if (arg == null || (arg.Connection != null && arg.Player() == null))
                {
                    return true;
                }
                if (arg.cmd.FullName == "chat.say" || arg.cmd.FullName == "chat.teamsay" || arg.cmd.FullName == "chat.localsay")
                {
                    return null;
                }
                string[] args = arg.FullString.SplitQuotesStrings(2147483647);
                object obj = BroadcastReturn("OnServerCommand", arg);
                object obj1 = BroadcastReturn("OnServerCommand", arg.cmd.FullName, args);
                if (obj == null ? obj1 == null : obj == null)
                {
                    return null;
                }
                return true;
            }
            return null;
        }

        internal object CanBeAwardedAdventGiftHook(AdventCalendar advent, BasePlayer player)
        {
            return BroadcastReturn("CanBeAwardedAdventGift", advent, player);
        }

        internal object OnAdventGiftAwardHook(AdventCalendar advent, BasePlayer player)
        {
            return BroadcastReturn("OnAdventGiftAward", advent, player);
        }

        internal void OnAdventGiftAwardedHook(AdventCalendar advent, BasePlayer player)
        {
            Broadcast("OnAdventGiftAwarded", advent, player);
        }

        //public object OnConsoleCommandHook(ConsoleSystem.Arg arg)
        internal object OnConsoleCommandHook(string command, object[] args)
        {
            OnChatCommandHook(null, command, args);
            if (Auxide.full)
            {
                Utils.DoLog($"OnConsoleCommandHook was called for command {command}");
                return BroadcastReturn("OnConsoleCommand", command, args);
            }
            return null;
        }

        internal void OnChatCommandHook(BasePlayer player, string chat, object[] args = null)
        {
            string[] hookArgs = chat.Split(' '); // Extract command and args
            string command = hookArgs[0].Replace("/", ""); // Cleanup command
            hookArgs = hookArgs.ToList().Skip(1).ToArray(); // Remove command portion from args
            bool serverCmd = false;
            if (Auxide.verbose)
            {
                //Utils.DoLog($"OnChatCommandHook called for {player?.displayName}, command '{command}', args '{string.Join(",", args)}'");
                Utils.DoLog($"OnChatCommandHook called for {player?.displayName}, command '{command}', args '{string.Join(",", hookArgs)}'");
            }

            if (player?.IsAdmin != false)
            {
                switch (command)
                {
                    case "a.version":
                    case "auxide.version":
                        {
                            serverCmd = true;
                            Assembly assem = Assembly.GetExecutingAssembly();
                            AssemblyName assemName = assem.GetName();
                            Version ver = assemName.Version;
                            if (player == null)
                            {
                                Utils.DoLog($"{assemName} {ver}");
                                break;
                            }
                            player?.ChatMessage($"{assemName} {ver}");
                        }
                        break;
                    case "a.verbose":
                    case "auxide.verbose":
                        {
                            serverCmd = true;
                            Auxide.verbose = !Auxide.verbose;
                            if (player == null)
                            {
                                Utils.DoLog($"Verbose is now {Auxide.verbose}");
                                break;
                            }
                            player?.ChatMessage($"Verbose is now {Auxide.verbose}");
                        }
                        break;
                    case "a.unload":
                    case "auxide.unload":
                        {
                            serverCmd = true;
                            if (hookArgs.Length == 1 && _scripts.TryGetValue(hookArgs[0], out Script script))
                            {
                                script.Dispose();
                                _scripts.Remove(hookArgs[0]);
                            }
                        }
                        break;
                    case "a.reload":
                    case "auxide.reload":
                        {
                            serverCmd = true;
                            if (hookArgs.Length == 1)
                            {
                                string scriptName = hookArgs[0].Replace(".dll", "");
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
                        break;
                    case "a.load":
                    case "auxide.load":
                        {
                            serverCmd = true;
                            if (hookArgs.Length == 1)
                            {
                                string scriptName = hookArgs[0].Replace(".dll", "");
                                if (!_scripts.TryGetValue(scriptName, out _))
                                {
                                    Script newScript = new Script(this, Path.Combine(Auxide.ScriptPath, $"{scriptName}.dll"));
                                    _scripts.Add(scriptName, newScript);
                                    UpdateScript(newScript, scriptName);
                                }
                            }
                        }
                        break;
                    case "a.info":
                    case "auxide.info":
                        {
                            serverCmd = true;
                            string verbose = Auxide.verbose.ToString();
                            //string useint = Auxide.useInternal.ToString();
                            string runMode = Auxide.full ? "full" : "minimal";
                            Assembly assem = Assembly.GetExecutingAssembly();
                            AssemblyName assemName = assem.GetName();
                            Version ver = assemName.Version;
                            string msg = $"{assemName} {ver}\nRun Mode: {runMode}\nVerboseLogging: {verbose}";
                            player?.ChatMessage(msg);
                        }
                        break;
                    case "a.list":
                    case "auxide.list":
                        {
                            serverCmd = true;
                            string mess = "";
                            foreach (KeyValuePair<string, Script> script in _scripts)
                            {
                                mess += $"{script.Key}, {script.Value.Instance.Version} {script.Value.Instance.Description}\n";
                            }
                            if (player == null)
                            {
                                Utils.DoLog(mess);
                                break;
                            }
                            player?.ChatMessage(mess);
                        }
                        break;
                    case "listgroupmembers":
                    case "listmembers":
                        {
                            serverCmd = true;
                            Dictionary<string, bool> members = Permissions.GetGroupMembers(hookArgs[0]);
                            string message = $"Group members for {hookArgs[0]}:";
                            foreach (KeyValuePair<string, bool> member in members)
                            {
                                string isgroup = member.Value ? " (group)" : "";
                                message += $"\t{member.Key}{isgroup}\n";
                            }
                            if (player == null)
                            {
                                Utils.DoLog(message);
                                break;
                            }
                            player?.ChatMessage(message);
                        }
                        break;
                    case "listgroups":
                        {
                            serverCmd = true;
                            List<string> groups = Permissions.GetGroups();
                            string message = "Groups:\n";
                            foreach (string group in groups)
                            {
                                message += $"\t{group}\n";
                            }
                            if (player == null)
                            {
                                Utils.DoLog(message);
                                break;
                            }
                            player?.ChatMessage(message);
                        }
                        break;
                    case "addgroup":
                    case "groupadd":
                        serverCmd = true;
                        if (hookArgs.Length == 1)
                        {
                            Permissions.AddGroup(hookArgs[0]);
                        }
                        break;
                    case "remgroup":
                    case "removegroup":
                        serverCmd = true;
                        if (hookArgs.Length == 1)
                        {
                            Permissions.RemoveGroup(hookArgs[0]);
                        }
                        break;
                    case "addtogroup":
                        serverCmd = true;
                        if (hookArgs.Length == 2)
                        {
                            Permissions.AddGroupMember(hookArgs[0], hookArgs[1]);
                        }
                        break;
                    case "removefromgroup":
                    case "remfromgroup":
                        serverCmd = true;
                        if (hookArgs.Length == 2)
                        {
                            Permissions.RemoveGroupMember(hookArgs[0], hookArgs[1]);
                        }
                        break;
                    case "addperm":
                    case "grantperm":
                    case "grant":
                        serverCmd = true;
                        if (hookArgs.Length == 2)
                        {
                            Permissions.GrantPermission(hookArgs[1], hookArgs[0]);
                        }
                        break;
                    case "remperm":
                    case "removeperm":
                    case "revoke":
                        serverCmd = true;
                        if (hookArgs.Length == 2)
                        {
                            Permissions.RevokePermission(hookArgs[1], hookArgs[0]);
                        }
                        break;
                    case "showperm":
                        serverCmd = true;
                        if (player == null)
                        {
                            Utils.DoLog(Permissions.ShowPermissions(hookArgs[0]));
                            break;
                        }
                        if (hookArgs.Length == 1)
                        {
                            player?.ChatMessage(Permissions.ShowPermissions(hookArgs[0]));
                        }
                        break;
                }
            }
            if (Auxide.full && !serverCmd) Broadcast("OnChatCommand", player, command, hookArgs);
        }
        #endregion

        internal IEnumerable<string> PopulateScriptReferences(RustScript rustScript)
        {
            Type type = rustScript.GetType();

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
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

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
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

        // Single-script call with no return, perhaps to be renamed as Target
        public void Narrowcast(string methodName, IScriptReference script)
        {
            //lock (_sync)
            //{
            script.InvokeProcedure(methodName);
            //}
        }

        // Single-script call with return
        public object NarrowcastReturn(string methodName, IScriptReference script)
        {
            //lock (_sync)
            //{
            object rtrn = script.InvokeFunction<object>(methodName);
            //}

            return rtrn;
        }

        public void Broadcast(string methodName)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    script.InvokeProcedure(methodName);
                }
            }
        }

        public void Broadcast<T0>(string methodName, T0 arg0)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    script.InvokeProcedure(methodName, arg0);
                }
            }
        }

        public void Broadcast<T0, T1>(string methodName, T0 arg0, T1 arg1)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    script.InvokeProcedure(methodName, arg0, arg1);
                }
            }
        }

        public void Broadcast<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    script.InvokeProcedure(methodName, arg0, arg1, arg2);
                }
            }
        }

        public void Broadcast<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    script.InvokeProcedure(methodName, arg0, arg1, arg2, arg3);
                }
            }
        }

        public void Broadcast<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
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
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    rtrn = script.InvokeFunction<object>(methodName);
                    if (lastRet != null && lastRet is bool && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script?.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script?.Name;
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
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    rtrn = script.InvokeFunction<object, T0>(methodName, arg0);
                    if (lastRet != null && lastRet is bool && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script?.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script?.Name;
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
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    rtrn = script.InvokeFunction<T0, T1, object>(methodName, arg0, arg1);
                    if (lastRet != null && lastRet is bool && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script?.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script?.Name;
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
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    rtrn = script.InvokeFunction<T0, T1, T2, object>(methodName, arg0, arg1, arg2);
                    if (lastRet != null && lastRet is bool && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script?.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script?.Name;
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
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    rtrn = script.InvokeFunction<T0, T1, T2, T3, object>(methodName, arg0, arg1, arg2, arg3);
                    if (lastRet != null && lastRet is bool && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script?.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script?.Name;
                }
                rtrn = lastRet;
            }

            return rtrn;
        }

        public object BroadcastReturn<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            object rtrn = null;
            object lastRet = null;
            string lastScript = "";

            lock (_sync)
            {
                foreach (Script script in _scripts.Values.Where(x => x.initialized))
                {
                    rtrn = script.InvokeFunction<T0, T1, T2, T3, T4, object>(methodName, arg0, arg1, arg2, arg3, arg4);
                    if (lastRet != null && lastRet is bool && !rtrn.Equals(lastRet))
                    {
                        Utils.DoLog($"Conflict between {lastScript} and {script?.Name} return values!");
                    }
                    lastRet = rtrn;
                    lastScript = script?.Name;
                }
                rtrn = lastRet;
            }

            return rtrn;
        }
    }
}
