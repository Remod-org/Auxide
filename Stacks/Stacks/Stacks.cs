using Auxide;
using Auxide.Scripting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class HStacks : RustScript
{
    #region vars
    private SortedDictionary<string, SortedDictionary<string, int>> cat2items = new SortedDictionary<string, SortedDictionary<string, int>>();
    private SortedDictionary<string, string> name2cat = new SortedDictionary<string, string>();
    private DynamicConfigFile bdata;
    private ConfigData configData;
    #endregion

    #region Message
    public string Lang(string input, params object[] args)
    {
        return string.Format(lang.Get(input), args);
    }

    public void Message(BasePlayer player, string input, params object[] args)
    {
        Utils.SendReply(player, string.Format(lang.Get(input), args));
    }
    #endregion

    public override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["notauthorized"] = "You don't have permission to use this command.",
            ["current"] = "Current stack size for {0} is {1}",
            ["stackset"] = "Stack size for {0} is now {1}",
            ["notexist"] = "Item {0} does not exist.",
            ["helptext1"] = "Type /stack itemname OR hold an entity and type /stack.",
            ["helptext2"] = "Categories:\n{0}Type /stcat CATEGORY to list items in that category.",
            ["itemlist"] = "Items in {0}:\n{1}\n  To set the stack size for all items in this category, add the value to the end of this command, e.g. /stcat traps 10\n  THIS IS NOT REVERSIBLE!",
            ["catstack"] = "The stack size for each item in category {0} was set to {1}.",
            ["invalid"] = "Invalid item {0} selected.",
            ["found"] = "Found {0} matching item(s)\n{1}",
            ["imported"] = "Imported {0} item(s)",
            ["exported"] = "Exported {0} item(s)",
            ["importfail"] = "Import failed :(",
            ["none"] = "None",
            ["invalidc"] = "Invalid category: {0}."
        }, Name);
    }

    public override void Initialize()
    {
        LoadConfig();
        LoadData();
        UpdateItemList();
    }

    private string GetHeldItem(BasePlayer player)
    {
        if (player == null) return null;

        HeldEntity held = player.GetHeldEntity();

        return held.ShortPrefabName == "planner" ? held.GetOwnerItemDefinition().name : held.GetItem().info.name;
    }

    private class Items
    {
        public Dictionary<string, int> itemlist = new Dictionary<string, int>();
    }

    public void OnChatCommand(BasePlayer player, string command, string[] args = null)
    {
        if (player == null) return;
        if (!player.IsAdmin) { Message(player, "notauthorized"); return; }

        switch (command)
        {
            case "stack":
                CmdStackInfo(player, command, args);
                break;
            case "stcat":
                CmdStackCats(player, command, args);
                break;
            case "stimport":
                CmdStackImport(player, command, args);
                break;
        }
    }

    private void CmdStackInfo(BasePlayer player, string command, string[] args)
    {
        string itemName;
        int stack;

        string debug = string.Join(",", args); DoLog($"{debug}");
        if (args.Length == 0)
        {
            // Command alone, no args: get current stack size for held item
            DoLog("0: No item name set.  Looking for held entity");
            itemName = GetHeldItem(player);

            if (itemName == null) { Message(player, "helptext1"); return; }

            if (name2cat.ContainsKey(itemName))
            {
                string cat = name2cat[itemName];
                string stackinfo = cat2items[cat][itemName].ToString();
                Message(player, "current", itemName, stackinfo);
            }
            else
            {
                Message(player, "invalid");
            }
        }
        else if (args.Length == 1)
        {
            DoLog("1: No item name set.  Looking for held entity.");
            int.TryParse(args[0], out stack);
            if (stack > 0)
            {
                DoLog($"Will set stack size to {stack} for held item.");
                itemName = GetHeldItem(player);
                if (name2cat.ContainsKey(itemName))
                {
                    //string cat = name2cat[itemName];
                    UpdateItem(itemName, stack);
                    Message(player, "stackset", itemName, stack.ToString());
                }
                else
                {
                    Message(player, "invalid");
                    return;
                }
            }
            else
            {
                DoLog("Item name set on command line: Get the current stack size for the named item.");
                itemName = args[0].Replace(".item", "") + ".item";

                if (name2cat.ContainsKey(itemName))
                {
                    string cat = name2cat[itemName];
                    string stackinfo = cat2items[cat][itemName].ToString();
                    Message(player, "current", itemName, stackinfo);
                }
                else
                {
                    Message(player, "invalid", itemName);
                    return;
                }
            }
        }
        else if (args.Length == 2)
        {
            if (args[0] == "search")
            {
                string res = "";
                int i = 0;
                foreach (string nm in name2cat.Keys)
                {
                    if (nm.IndexOf(args[1], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        i++;
                        string cat = name2cat[nm];
                        string stackinfo = cat2items[cat][nm].ToString();
                        res += $"\t[{name2cat[nm]}] {nm} ({stackinfo})\n";
                    }
                }
                Message(player, "found", i.ToString(), res);
                return;
            }

            DoLog("Item name AND stack size set on command line: Set the stack size for the named item.");
            itemName = args[0].Replace(".item", "") + ".item";
            //if (itemName.Length < 5)
            //{
            //    itemName += ".item";
            //}
            //else
            //{
            //    if (itemName.Substring(itemName.Length - 5) != ".item")
            //    {
            //        itemName += ".item";
            //    }
            //}

            if (name2cat.ContainsKey(itemName))
            {
                int.TryParse(args[1], out stack);
                DoLog($"Will set stack size to {stack}");
                if (stack > 0)
                {
                    UpdateItem(itemName, stack);
                    Message(player, "stackset", itemName, stack.ToString());
                }
            }
        }
    }

    private void CmdStackCats(BasePlayer iplayer, string command, string[] args)
    {
        string debug = string.Join(",", args); DoLog($"{debug}");

        if (args.Length == 0)
        {
            string cats = "\t" + string.Join("\n\t", cat2items.Keys.ToList()) + "\n";
            Message(iplayer, "helptext2", cats);
        }
        else if (args.Length == 1)
        {
            string items = null;
            if (!cat2items.ContainsKey(args[0]))
            {
                Message(iplayer, "invalidc", args[0]);
                return;
            }
            foreach (KeyValuePair<string, int> item in cat2items[args[0]])
            {
                items += $"\t{item.Key}: {item.Value}\n";
            }
            Message(iplayer, "itemlist", args[0], items);
        }
        else if (args.Length == 2)
        {
            if (!cat2items.ContainsKey(args[0])) return;
            foreach (KeyValuePair<string, int> item in cat2items[args[0]])
            {
                int.TryParse(args[1], out int stack);
                if (stack > 0)
                {
                    UpdateItem(item.Key, stack);
                }
            }
            Message(iplayer, "catstack", args[0], args[1]);
        }
    }

    private void CmdStackImport(BasePlayer iplayer, string command, string[] args)
    {
        Utils.DoLog("Opening data file for SSC");
        DynamicConfigFile ssc = data.GetFile("StackSizeController");
        Items sscitems = ssc.ReadObject<Items>();
        Utils.DoLog("Successfully read object");
        List<ItemDefinition> itemList = ItemManager.GetItemDefinitions();

        cat2items = new SortedDictionary<string, SortedDictionary<string, int>>();
        name2cat = new SortedDictionary<string, string>();
        ItemDefinition id = null;

        int i = 0;
        int stack = 0;
        foreach (KeyValuePair<string, int> item in sscitems.itemlist)
        {
            Utils.DoLog($"Processing '{item.Key}' in SSC itemlist.");

            foreach (ItemDefinition idef in itemList)
            {
                if (idef.displayName.english == item.Key)
                {
                    id = idef;
                    stack = item.Value;
                    break;
                }
            }
            if (id == null || stack == 0) continue;

            string nm = id.name;
            string cat = id.category.ToString().ToLower();

            Utils.DoLog($"{item.Key} ({nm}): category {cat}, stack size {stack}");

            if (!cat2items.ContainsKey(cat))
            {
                // Category missing, create it
                cat2items.Add(cat, new SortedDictionary<string, int>());
            }

            cat2items[cat].Add(nm, stack);

            name2cat.Add(nm, cat);
            i++;
        }
        if (i > 0)
        {
            Message(iplayer, "imported", i.ToString());
            SaveData();
            LoadData();
        }
        else
        {
            Message(iplayer, "importfail");
        }
    }


    private void LoadData()
    {
        DoLog("LoadData called");
        //data = data.GetFile("/stacking");
        cat2items = data.ReadObject<SortedDictionary<string, SortedDictionary<string, int>>>(Name);
        //playerHomes = data.ReadObject<Dictionary<ulong, HomeData>>(Name);

        //foreach (KeyValuePair<string, SortedDictionary<string, int>> itemcats in cat2items)
        //{
        //    foreach (KeyValuePair<string, int> items in itemcats.Value)
        //    {
        //        IEnumerable<ItemDefinition> itemenum = ItemManager.GetItemDefinitions().Where(x => x.name.Equals(itemcats.Key));
        //        ItemDefinition item = itemenum?.FirstOrDefault();
        //        if (item != null)
        //        {
        //            DoLog($"Setting stack size for {item?.name} to {items.Value.ToString()}");
        //            item.stackable = items.Value;
        //        }
        //    }
        //}

        bdata = data.GetFile("name2cat");
        name2cat = bdata.ReadObject<SortedDictionary<string, string>>();
    }

    private void SaveData()
    {
        data.WriteObject(Name, cat2items);
        bdata.WriteObject(name2cat);
    }

    private void DoLog(string message)
    {
        if (configData.Options.debug) Utils.DoLog(message);
    }

    private void UpdateItem(string itemName, int stack)
    {
        List<ItemDefinition> itemDefs = ItemManager.GetItemDefinitions();
        IEnumerable<ItemDefinition> itemDefIe = from rv in itemDefs where rv.name.Equals(itemName) select rv;

        ItemDefinition itemDef = itemDefIe.FirstOrDefault();
        DoLog($"Attempting to set stack size of {itemName} to {stack}");
        itemDef.stackable = stack;
        UpdateItemList(itemName, stack);
    }

    private void UpdateItemList(string itemNameToSet = null, int stack = 0)
    {
        DoLog("Update ItemList");
        if (itemNameToSet != null && stack > 0)
        {
            // Update one item
            string cat = name2cat[itemNameToSet];
            if (cat != null && !cat2items.ContainsKey(cat))
            {
                // Category missing, create it
                cat2items.Add(cat, new SortedDictionary<string, int>());
            }

            if (cat2items[cat].ContainsKey(itemNameToSet))
            {
                cat2items[cat][itemNameToSet] = stack;
                SaveData();
            }
            return;
        }

        SortedDictionary<string, SortedDictionary<string, int>> oldcat2items = cat2items;
        cat2items = new SortedDictionary<string, SortedDictionary<string, int>>();
        DoLog("Re-populating saved list");
        foreach (KeyValuePair<int, ItemDefinition> itemdef in ItemManager.itemDictionary)
        {
            string cat = itemdef.Value.category.ToString().ToLower();
            if (cat.Length == 0) continue;

            string itemName = itemdef.Value.name;
            int stackable = itemdef.Value.stackable;

            if (!cat2items.ContainsKey(cat))
            {
                DoLog($"Adding category: {cat}");
                cat2items.Add(cat, new SortedDictionary<string, int>());
            }

            if (oldcat2items.Count > 0)
            {
                if (oldcat2items[cat].ContainsKey(itemName))
                {
                    // Preserve previously set stacksize
                    int oldstack = oldcat2items[cat][itemName];
                    if (oldstack > 0 && !cat2items[cat].ContainsKey(itemName))
                    {
                        DoLog($"Setting stack size for {itemName} to previously set value {oldstack}");
                        cat2items[cat].Add(itemName, oldstack);
                        itemdef.Value.stackable = oldstack;
                    }
                }
            }
            else
            {
                if (!cat2items[cat].ContainsKey(itemName))
                {
                    DoLog($"Setting new stack size for {itemName} to default of {stackable}");
                    cat2items[cat].Add(itemName, stackable);
                    itemdef.Value.stackable = stackable;
                }
            }

            if (!name2cat.ContainsKey(itemName)) name2cat.Add(itemName, cat);
        }
        SaveData();
    }

    #region config
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
        Utils.DoLog("Creating new config file.");
        ConfigData cfg = new ConfigData
        {
            Options = new Options()
            {
                debug = false,
                maxStack = 100000
            },
            Version = Version
        };

        SaveConfig(cfg);
    }

    private void SaveConfig(ConfigData cfg)
    {
        config.WriteObject(cfg);
    }

    public class ConfigData
    {
        public Options Options;
        public VersionNumber Version;
    }

    public class Options
    {
        [JsonProperty(PropertyName = "Enable debugging")]
        public bool debug;

        [JsonProperty(PropertyName = "Maximum allowable stack size")]
        public int maxStack;
    }
    #endregion
}
