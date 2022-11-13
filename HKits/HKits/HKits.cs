using Auxide;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class HKits : RustScript
{
    private static ConfigData configData;
    private static Dictionary<string, Kit> kits = new Dictionary<string, Kit>();
    private bool newsave;

    public HKits()
    {
        Author = "RFC1920";
        Description = "Basic user kits for Auxide";
        Version = new VersionNumber(1, 0, 1);
    }

    public class ConfigData
    {
        public bool debug;
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

    class Kit
    {
        public string name;
        public string description;
        public List<KitItem> items;
    }

    class KitItem
    {
        public ItemLocation location;
        public int count;
        public int itemid;
        public ulong skinid;
        public string name;
    }

    enum ItemLocation
    {
        wear = 0,
        belt = 1,
        main = 2
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
            ["notauthorized"] = "You don't have permission to use this command."
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
            debug = false
        };

        config.WriteObject(configData);
    }

    public void OnNewSave()
    {
        newsave = true;
    }

    public void OnChatCommand(BasePlayer player, string command, string[] args = null)
    {
        if (player == null) return;

        Utils.DoLog($"Args length is {args.Length}");
        switch (command)
        {
            case "kit":
                if (args.Length == 2)
                {
                    if (kits.ContainsKey(args[1]))
                    {
                        Kit kit = kits[args[1]];
                        foreach (KitItem item in kit.items.Where(x => x.location == ItemLocation.wear))
                        {
                            Item newitem = ItemManager.CreateByItemID(item.itemid, item.count, item.skinid);
                            newitem.MoveToContainer(player.inventory.containerWear, -1, true, false);
                        }
                        foreach (KitItem item in kit.items.Where(x => x.location == ItemLocation.belt))
                        {
                            Item newitem = ItemManager.CreateByItemID(item.itemid, item.count, item.skinid);
                            newitem.MoveToContainer(player.inventory.containerBelt, -1, true, false);
                        }
                        foreach (KitItem item in kit.items.Where(x => x.location == ItemLocation.main))
                        {
                            Item newitem = ItemManager.CreateByItemID(item.itemid, item.count, item.skinid);
                            newitem.MoveToContainer(player.inventory.containerMain, -1, true, false);
                        }
                    }
                }
                else if (args.Length == 2 && args[1] == "list")
                {
                    string message = Lang("kits") + "\n";
                    foreach (string kit in kits.Keys)
                    {
                        message += $"{kit}\n";
                    }
                    Message(player, message);
                }
                else if (args.Length == 3 && args[1] == "create" && player.IsAdmin)
                {
                    Kit newkit = new Kit()
                    {
                        name = args[2],
                        description = args[2],
                        items = new List<KitItem>()
                    };

                    foreach (Item item in player.inventory.containerWear.itemList)
                    {
                        newkit.items.Add(new KitItem
                        {
                            location = ItemLocation.wear,
                            count = item.amount,
                            itemid = item.info.itemid,
                            skinid = item.skin,
                            name = item.info.displayName.english
                        });
                    }
                    foreach (Item item in player.inventory.containerBelt.itemList)
                    {
                        newkit.items.Add(new KitItem
                        {
                            location = ItemLocation.belt,
                            count = item.amount,
                            itemid = item.info.itemid,
                            skinid = item.skin,
                            name = item.info.displayName.english
                        });
                    }
                    foreach (Item item in player.inventory.containerMain.itemList)
                    {
                        newkit.items.Add(new KitItem
                        {
                            location = ItemLocation.main,
                            count = item.amount,
                            itemid = item.info.itemid,
                            skinid = item.skin,
                            name = item.info.displayName.english
                        });
                    }
                    kits.Add(args[2], newkit);
                    SaveData();
                }
                break;
        }
    }

    private void SaveData()
    {
        data.WriteObject("kits", kits);
    }

    private void LoadData()
    {
        if (newsave)
        {
            newsave = false;
            kits = new Dictionary<string, Kit>();
            SaveData();
            return;
        }
        else
        {
            kits = data.ReadObject<Dictionary<string, Kit>>("kits");
        }
        if (kits == null)
        {
            kits = new Dictionary<string, Kit>();
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

