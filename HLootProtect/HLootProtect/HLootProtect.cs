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
        //if (CheckCupboardAccess(ent, player)) return null;
        if (CanAccess(ent.ShortPrefabName, player.userID, ent.OwnerID)) return null;
        if (CheckShare(ent, player.userID)) return null;

        return null;
    }

    public object CanLoot(PlayerCorpse corpse, BasePlayer player, string panelName)
    {
        return null;
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

        Utils.DoLog($"Args length is {args.Length}");
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
                            Utils.SendReply(player, $"Shared {ename} with all");
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
                                    Utils.SendReply(player, $"lpshareinfo: {message}");
                                }
                            }
                            else
                            {
                                Utils.SendReply(player, "nonefound");
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
                                Utils.SendReply(player, $"sharedf {ename}");
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
                                Utils.SendReply(player, $"Shared {ename} with {sharewith.displayName}");
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
                            Utils.SendReply(player, "removeshare");
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

