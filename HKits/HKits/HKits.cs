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
    private List<ulong> isopen = new List<ulong>();
    private const string KITGUI = "hkits.gui";

    public HKits()
    {
        Author = "RFC1920";
        Description = "Basic user kits for Auxide";
        Version = new VersionNumber(1, 0, 1);
    }

    public class ConfigData
    {
        public bool debug;
        public bool usePermissions;
    }

    public override void Initialize()
    {
        LoadConfig();
        LoadData();

        foreach (BasePlayer player in BasePlayer.activePlayerList)
        {
            CuiHelper.DestroyUi(player, KITGUI);
            if (isopen.Contains(player.userID)) isopen.Remove(player.userID);
        }
    }

    public void OnScriptLoaded()
    {
        Utils.DoLog("OnScriptLoaded called for HKits");
        Permissions.RegisterPermission(Name, "kits.use");
    }

    public override void Dispose()
    {
        SaveData();
        foreach (BasePlayer player in BasePlayer.activePlayerList)
        {
            CuiHelper.DestroyUi(player, KITGUI);
            if (isopen.Contains(player.userID)) isopen.Remove(player.userID);
        }
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
            ["kits"] = "Kits:\n",
            ["kitgui"] = "Available Kits",
            ["kithelp"] = "Type /kit list to list kits, /kit NAME to select a kit, or /kits to bring up gui.",
            ["created"] = "Kit %1 has been created from your inventory.",
            ["issued"] = "Kit %1 has been issued to you.",
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
        if (configData.usePermissions && !Permissions.UserHasPermission("kits.use", player.UserIDString))
        {
            Message(player, "notauthorized");
            return;
        }
        switch (command)
        {
            case "kits":
                KitGUI(player);
                break;
            case "kit":
                if (args.Length == 2)
                {
                    if (args[1] == "close")
                    {
                        CuiHelper.DestroyUi(player, KITGUI);
                        IsOpen(player.userID, false);
                        return;
                    }
                    if (args[1] == "list")
                    {
                        string message = Lang("kits");
                        foreach (string kit in kits.Keys)
                        {
                            message += $"{kit}\n";
                        }
                        Message(player, message);
                        return;
                    }

                    if (kits.ContainsKey(args[1]))
                    {
                        CuiHelper.DestroyUi(player, KITGUI);
                        IsOpen(player.userID, false);
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
                        return;
                    }
                    Message(player, "kithelp");
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

    private void IsOpen(ulong uid, bool set = false)
    {
        if (set)
        {
            Utils.DoLog($"Setting isopen for {uid}");
            if (!isopen.Contains(uid)) isopen.Add(uid);
            return;
        }
        Utils.DoLog($"Clearing isopen for {uid}");
        isopen.Remove(uid);
    }

    private float[] GetButtonPosition(int rowNumber, int columnNumber)
    {
        float offsetX = 0.05f + (0.096f * columnNumber);
        float offsetY = (0.80f - (rowNumber * 0.064f));

        return new float[] { offsetX, offsetY, offsetX + 0.196f, offsetY + 0.03f };
    }

    private float[] GetButtonPositionP(int rowNumber, int columnNumber)
    {
        float offsetX = 0.05f + (0.126f * columnNumber);
        float offsetY = (0.87f - (rowNumber * 0.064f));

        return new float[] { offsetX, offsetY, offsetX + 0.226f, offsetY + 0.03f };
    }

    private void KitGUI(BasePlayer player)
    {
        if (player == null) return;
        IsOpen(player.userID, true);
        CuiHelper.DestroyUi(player, KITGUI);

        string description = Lang("kitgui");
        CuiElementContainer container = UI.Container(KITGUI, UI.Color("242424", 1f), "0.1 0.1", "0.9 0.9", true, "Overlay");
        UI.Label(ref container, KITGUI, UI.Color("#ffffff", 1f), description, 18, "0.23 0.92", "0.7 1");
        UI.Button(ref container, KITGUI, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", "kit close");

        int col = 0;
        int row = 0;

        foreach (KeyValuePair<string, Kit> kit in kits)
        {
            if (row > 10)
            {
                row = 0;
                col++; col++;
            }
            float[] posb = GetButtonPositionP(row, col);

            UI.Button(ref container, KITGUI, UI.Color("#424242", 1f), kit.Value.name, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"kit {kit.Value.name}");
            col++;
            posb = GetButtonPositionP(row, col);
            UI.Label(ref container, KITGUI, UI.Color("#424242", 1f), kit.Value.description, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}");
            col--;
            row++;
        }

        CuiHelper.AddUi(player, container);
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

