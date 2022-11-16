using Auxide;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class HLootProtect : RustScript
{
    private static ConfigData configData;
    private Dictionary<ulong, List<Share>> sharing = new Dictionary<ulong, List<Share>>();
    private Dictionary<string, long> lastConnected = new Dictionary<string, long>();
    private Dictionary<ulong, ulong> lootingBackpack = new Dictionary<ulong, ulong>();
    private bool newsave;

    public HLootProtect()
    {
        Author = "RFC1920";
        Description = "Basic loot protection for Auxide";
        Version = new VersionNumber(1, 0, 1);
    }

    public class ConfigData
    {
        public bool debug;
        public bool HonorRelationships;
        public bool protectCorpse;
        public bool TCAuthedUserAccess;
        public float protectedDays;
        public Dictionary<string, bool> Rules = new Dictionary<string, bool>();
    }

    public class Share
    {
        public string name;
        public uint netid;
        public ulong sharewith;
    }

    public override void Initialize()
    {
        LoadConfig();
        LoadData();
    }

    public override void Dispose()
    {
        SaveData();
        base.Dispose();
    }

    public string Lang(string input, params object[] args)
    {
        return string.Format(lang.Get(input), args);
    }

    public void Message(BasePlayer player, string input, params object[] args)
    {
        Utils.SendReply(player, string.Format(lang.Get(input), args));
    }

    public override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["checkinglocal"] = "[LootProtect] Checking {0} local entities",
            ["enabled"] = "LootProtect enabled.",
            ["disabled"] = "LootProtect disabled.",
            ["status"] = "LootProtect enable is set to {0}.",
            ["logging"] = "Logging set to {0}",
            ["all"] = "all",
            ["friends"] = "friends",
            ["nonefound"] = "No entity found.",
            ["settings"] = "{0} Settings:\n{1}",
            ["shared"] = "{0} shared with {1}.",
            ["sharedf"] = "{0} shared with friends.",
            ["removeshare"] = "Sharing removed.",
            ["removesharefor"] = "Sharing removed for {0} entities.",
            ["shareinfo"] = "Share info for {0}",
            ["lpshareinfo"] = "[LootProtect] Share info for {0}",
            ["notauthorized"] = "You don't have permission to use this command.",
        }, Name);
    }

    public void LoadConfig()
    {
        if (config.Exists())
        {
            configData = config.ReadObject<ConfigData>();
            return;
        }
        LoadDefaultConfig();
    }

    public void LoadDefaultConfig()
    {
        configData = new ConfigData()
        {
            debug = false,
            HonorRelationships = true,
            protectCorpse = true,
            TCAuthedUserAccess = true,
            protectedDays = 0f,
            Rules = new Dictionary<string, bool>
            {
                { "box.wooden.large", true },
                { "button", true },
                { "item_drop_backpack", true },
                { "woodbox_deployed", true },
                { "bbq.deployed",     true },
                { "fridge.deployed",  true },
                { "workbench1.deployed", true },
                { "workbench2.deployed", true },
                { "workbench3.deployed", true },
                { "cursedcauldron.deployed", true },
                { "campfire",      true },
                { "furnace.small", true },
                { "furnace.large", true },
                { "player",        true },
                { "player_corpse", true },
                { "scientist_corpse", false },
                { "murderer_corpse", false },
                { "fuelstorage", true },
                { "hopperoutput", true },
                { "recycler_static", false },
                { "sign.small.wood", true},
                { "sign.medium.wood", true},
                { "sign.large.wood", true},
                { "sign.huge.wood", true},
                { "sign.pictureframe.landscape", true},
                { "sign.pictureframe.portrait", true},
                { "sign.hanging", true},
                { "sign.pictureframe.tall", true},
                { "sign.pictureframe.xl", true},
                { "sign.pictureframe.xxl", true},
                { "repairbench_deployed", false },
                { "refinery_small_deployed", false },
                { "researchtable_deployed", false },
                { "mixingtable.deployed", false },
                { "vendingmachine.deployed", false },
                { "lock.code", true },
                { "lock.key", true },
                { "abovegroundpool.deployed", true },
                { "paddlingpool.deployed", true }
            }
        };

        config.WriteObject(configData);
    }

    public void OnNewSave()
    {
        newsave = true;
    }

    public object CanLoot(StorageContainer container, BasePlayer player, string panelName)
    {
        if (player == null || container == null) return null;
        BaseEntity ent = container?.GetComponentInParent<BaseEntity>();
        if (ent == null) return null;
        Utils.DoLog($"Player {player.displayName} looting StorageContainer {ent.ShortPrefabName}");
        if (CheckCupboardAccess(ent, player)) return null;
        if (CanAccess(ent.ShortPrefabName, player.userID, ent.OwnerID)) return null;
        if (CheckShare(ent, player.userID)) return null;

        return null;
    }

    public object CanLoot(PlayerCorpse corpse, BasePlayer player, string panelName)
    {
        if (player == null || corpse == null) return null;
        Utils.DoLog($"Player {player.displayName}:{player.UserIDString} looting corpse {corpse.name}:{corpse.playerSteamID}");
        if (CanAccess(corpse.ShortPrefabName, player.userID, corpse.playerSteamID)) return null;

        return true;
    }

    public void OnChatCommand(BasePlayer player, string command, string[] args = null)
    {
        if (player == null) return;

        //string debug = string.Join(",", args); Utils.DoLog($"{command} {debug}");
        if (!sharing.ContainsKey(player.userID))
        {
            Utils.DoLog($"Creating new sharing data for {player.displayName}");
            sharing.Add(player.userID, new List<Share>());
            SaveData();
        }

        switch (command)
        {
            case "share":
                if (args.Length == 1)
                {
                    if (Physics.Raycast(player.eyes.HeadRay(), out RaycastHit hit, 2.2f))
                    {
                        BaseEntity ent = hit.GetEntity();
                        if (ent != null)
                        {
                            if (ent.OwnerID != player.userID && !Utils.IsFriend(player.userID, ent.OwnerID)) return;
                            string ename = ent.ShortPrefabName;
                            sharing[player.userID].Add(new Share { netid = ent.net.ID, name = ename, sharewith = 0 });
                            SaveData();
                            //Utils.SendReply(player, $"Shared {ename} with all");
                            Message(player, "shared", ename, Lang("all"));
                        }
                    }
                }
                else if (args.Length == 2)
                {
                    if (args[1] == "?")
                    {
                        if (Physics.Raycast(player.eyes.HeadRay(), out RaycastHit hit, 2.2f))
                        {
                            BaseEntity ent = hit.GetEntity();
                            string message = "";
                            if (ent != null)
                            {
                                if (ent.OwnerID != player.userID && !Utils.IsFriend(player.userID, ent.OwnerID)) return;
                                // SHOW SHARED BY, KEEP IN MIND WHO OWNS BUT DISPLAY IF FRIEND, ETC...
                                if (sharing.ContainsKey(ent.OwnerID))
                                {
                                    string ename = ent.ShortPrefabName;
                                    message += $"{ename}({ent.net.ID}):\n";
                                    foreach (Share x in sharing[ent.OwnerID])
                                    {
                                        if (x.netid != ent.net.ID) continue;
                                        if (x.sharewith == 0)
                                        {
                                            message += "\t" + "all" + "\n";
                                        }
                                        else if (x.sharewith == 1)
                                        {
                                            message += "\t" + "friends" + "\n";
                                        }
                                        else
                                        {
                                            message += $"\t{x.sharewith}\n";
                                        }
                                    }
                                    //Utils.SendReply(player, $"lpshareinfo: {message}");
                                    Message(player, "lpshareinfo", message);
                                }
                            }
                            else
                            {
                                //Utils.SendReply(player, "nonefound");
                                Message(player, "nonefound");
                            }
                        }
                    }
                    else if (args[1] == "friends")
                    {
                        if (!configData.HonorRelationships) return;
                        if (Physics.Raycast(player.eyes.HeadRay(), out RaycastHit hit, 2.2f))
                        {
                            BaseEntity ent = hit.GetEntity();
                            if (ent != null)
                            {
                                if (ent.OwnerID != player.userID && !Utils.IsFriend(player.userID, ent.OwnerID)) return;
                                string ename = ent.ShortPrefabName;
                                sharing[player.userID].Add(new Share { netid = ent.net.ID, name = ename, sharewith = 1 });
                                SaveData();
                                //Utils.SendReply(player, $"sharedf {ename}");
                                Message(player, "sharedf", ename);
                            }
                        }
                    }
                    else
                    {
                        BasePlayer sharewith = FindPlayerByName(args[1]);
                        if (Physics.Raycast(player.eyes.HeadRay(), out RaycastHit hit, 2.2f))
                        {
                            BaseEntity ent = hit.GetEntity();
                            if (ent != null)
                            {
                                if (ent.OwnerID != player.userID && !Utils.IsFriend(player.userID, ent.OwnerID)) return;
                                string ename = ent.ShortPrefabName;
                                if (sharewith == null)
                                {
                                    if (!configData.HonorRelationships) return;
                                    sharing[player.userID].Add(new Share { netid = ent.net.ID, name = ename, sharewith = 1 });
                                }
                                else
                                {
                                    sharing[player.userID].Add(new Share { netid = ent.net.ID, name = ename, sharewith = sharewith.userID });
                                }
                                SaveData();
                                //Utils.SendReply(player, $"Shared {ename} with {sharewith.displayName}");
                                Message(player, "shared", ename, sharewith.displayName);
                            }
                        }
                    }
                }
                break;
            case "unshare":
                if (args.Length == 1)
                {
                    if (Physics.Raycast(player.eyes.HeadRay(), out RaycastHit hit, 2.2f))
                    {
                        BaseEntity ent = hit.GetEntity();
                        if (ent != null)
                        {
                            if (ent.OwnerID != player.userID && !Utils.IsFriend(player.userID, ent.OwnerID)) return;
                            List<Share> repl = new List<Share>();
                            foreach (Share x in sharing[player.userID])
                            {
                                if (x.netid != ent.net.ID)
                                {
                                    repl.Add(x);
                                }
                                else
                                {
                                    Utils.DoLog($"Removing {ent.net.ID} from sharing list...");
                                }
                            }
                            sharing[player.userID] = repl;
                            SaveData();
                            //LoadData();
                            //Utils.SendReply(player, "removeshare");
                            Message(player, "removeshare");
                        }
                    }
                }
                break;

        }
    }

    private void SaveData()
    {
        data.WriteObject("sharing", sharing);
        data.WriteObject("lastConnected", lastConnected);
    }

    private void LoadData()
    {
        if (newsave)
        {
            newsave = false;
            lastConnected = new Dictionary<string, long>();
            sharing = new Dictionary<ulong, List<Share>>();
            SaveData();
            return;
        }
        else
        {
            lastConnected = data.ReadObject<Dictionary<string, long>>("lastConnected");
            sharing = data.ReadObject<Dictionary<ulong, List<Share>>>("sharing");
        }
        if (sharing == null)
        {
            sharing = new Dictionary<ulong, List<Share>>();
            SaveData();
        }
    }

    private static BasePlayer FindPlayerByName(string name)
    {
        BasePlayer result = null;
        foreach (BasePlayer current in BasePlayer.activePlayerList)
        {
            if (current.displayName.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || current.UserIDString.Contains(name, CompareOptions.OrdinalIgnoreCase)
                    || current.displayName.Contains(name, CompareOptions.OrdinalIgnoreCase))
            {
                result = current;
            }
        }
        return result;
    }

    private bool CheckCupboardAccess(BaseEntity entity, BasePlayer player)
    {
        if (!configData.TCAuthedUserAccess) return false;

        BuildingPrivlidge tc = entity.GetBuildingPrivilege();
        if (tc == null)
        {
            Utils.DoLog($"CheckCupboardAccess:     Unable to find building privilege in range of {entity.ShortPrefabName}.");
            return false; // NO TC to check...
        }

        foreach (ProtoBuf.PlayerNameID p in tc.authorizedPlayers.ToArray())
        {
            float distance = (float)Math.Round(Vector3.Distance(tc.transform.position, entity.transform.position), 2);
            if (p.userid == player.userID)
            {
                Utils.DoLog($"CheckCupboardAccess:     Found authorized cupboard {distance}m from {entity.ShortPrefabName}!");
                return true;
            }
        }

        Utils.DoLog($"CheckCupboardAccess:     Unable to find authorized cupboard for {entity.ShortPrefabName}.");
        return false;
    }

    private bool CheckShare(BaseEntity target, ulong userid)
    {
        if (sharing.ContainsKey(target.OwnerID))
        {
            Utils.DoLog($"Found entry for {target.OwnerID}");
            foreach (Share x in sharing[target.OwnerID])
            {
                if (x.netid == target.net.ID && (x.sharewith == userid || x.sharewith == 0))
                {
                    Utils.DoLog($"Found netid {target.net.ID} shared to {userid} or all.");
                    return true;
                }
                if (Utils.IsFriend(target.OwnerID, userid))
                {
                    Utils.DoLog($"{userid} is friend of {target.OwnerID}");
                    return true;
                }
            }
        }
        return false;
    }

    // Main access check function
    private bool CanAccess(string prefab, ulong source, ulong target)
    {
        // The following skips a ton of logging if the user has their own backpack open.
        if (lootingBackpack.ContainsKey(source)) return true;

        if (configData.protectedDays > 0 && target > 76560000000000000L)
        {
            lastConnected.TryGetValue(target.ToString(), out long lc);
            if (lc > 0)
            {
                long now = ToEpochTime(DateTime.UtcNow);
                float days = Math.Abs((now - lc) / 86400);
                if (days > configData.protectedDays)
                {
                    Utils.DoLog($"Allowing access to container owned by player offline for {configData.protectedDays} days");
                    return true;
                }
                else
                {
                    Utils.DoLog($"Owner was last connected {days} days ago and is still protected...");
                    // Move on to the remaining checks...
                }
            }
        }

        BasePlayer player = BasePlayer.FindByID(source);
        if (player == null) return true;

        Utils.DoLog($"Checking access to {prefab}");
        //if (target == 0)
        if (target < 76560000000000000L)
        {
            Utils.DoLog("Not owned by a real player.  Access allowed.");
            return true;
        }
        if (source == target)
        {
            Utils.DoLog("Player-owned.  Access allowed.");
            return true;
        }
        if (Utils.IsFriend(source, target))
        {
            Utils.DoLog("Friend-owned.  Access allowed.");
            return true;
        }

        // Check protection rules since there is no relationship to the target owner.
        if (configData.Rules.ContainsKey(prefab))
        {
            if (configData.Rules[prefab])
            {
                Utils.DoLog($"Rule found for type {prefab}.  Access BLOCKED!");
                return false;
            }
            Utils.DoLog($"Rule found for type {prefab}.  Access allowed.");
            return true;
        }

        return false;
    }

    private long ToEpochTime(DateTime dateTime)
    {
        DateTime date = dateTime.ToUniversalTime();
        long ticks = date.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0).Ticks;
        return ticks / TimeSpan.TicksPerSecond;
    }

    //private BasePlayer FindPlayerByID(ulong userid)
    //{
    //    foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
    //    {
    //        if (activePlayer.userID.Equals(userid))
    //        {
    //            return activePlayer;
    //        }
    //    }
    //    foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
    //    {
    //        if (sleepingPlayer.userID.Equals(userid))
    //        {
    //            return sleepingPlayer;
    //        }
    //    }
    //    return null;
    //}
}

