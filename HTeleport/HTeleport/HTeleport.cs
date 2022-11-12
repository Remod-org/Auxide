using Auxide;
using Network;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class HTeleport : RustScript
{
    private static ConfigData configData;
    private static Dictionary<ulong, TpTimer> playerTP = new Dictionary<ulong, TpTimer>();
    private static Dictionary<ulong, HomeData> playerHomes = new Dictionary<ulong, HomeData>();

    public HTeleport()
    {
        Author = "RFC1920";
        Version = new VersionNumber(1, 0, 2);
    }

    public class HomeData
    {
        [JsonProperty("l")]
        public Dictionary<string, Vector3> Locations { get; set; } = new Dictionary<string, Vector3>();

        [JsonProperty("t")]
        public TeleportData Teleports { get; set; } = new TeleportData();
    }

    public class TeleportData
    {
        [JsonProperty("a")]
        public int Amount { get; set; }

        [JsonProperty("d")]
        public string Date { get; set; }

        [JsonProperty("t")]
        public int Timestamp { get; set; }
    }
    public class ConfigData
    {
        public bool debug;
        public float countdownSeconds;
        public HomeData server;
    }

    public void SaveConfig(ConfigData configuration)
    {
        config.WriteObject(configuration);
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
            countdownSeconds = 5,
            server = new HomeData()
        };

        configData.server.Locations.Add("town", default);
        configData.server.Locations.Add("bandit", default);
        configData.server.Locations.Add("outpost", default);

        SaveConfig(configData);
    }

    public override void Initialize()
    {
        LoadConfig();
        LoadData();
        FindMonuments();
    }

    public class TpTimer
    {
        public BasePlayer player;
        public Vector3 target;
        public System.Timers.Timer timer;
    }

    //public void LoadLang()
    //{
    //    lang.ReadObject<>(Name, "en");
    //}
    public void LoadData()
    {
        playerHomes = data.ReadObject<Dictionary<ulong, HomeData>>(Name);
    }

    public void SaveData()
    {
        data.WriteObject(Name, playerHomes);
    }

    public void OnChatCommand(BasePlayer player, string command, string[] args = null)
    {
        //string arginfo = string.Join(",", args);
        //Utils.DoLog($"Heard: {command}/{arginfo}");

        if (!playerHomes.ContainsKey(player.userID))
        {
            playerHomes.Add(player.userID, new HomeData());
            SaveData();
        }

        switch (command)
        {
            case "town":
                {
                    if (args.Length < 2 &&configData.server.Locations["town"] != default && configData.server.Locations["town"] != null)
                    {
                        Utils.SendReply(player, $"Teleporting to town in {configData.countdownSeconds} seconds");

                        AddTimer(player, configData.server.Locations["town"]);
                        return;
                    }

                    if (args[1] == "set" && player.IsAdmin)
                    {
                        configData.server.Locations["town"] = player.transform.position;
                        SaveConfig(configData);
                        Utils.SendReply(player, "Town set!");
                        return;
                    }
                }
                break;
            case "outpost":
                {
                    if (configData.server.Locations["outpost"] != default && configData.server.Locations["outpost"] != null)
                    {
                        if (configData.debug) Utils.DoLog($"Player {player.displayName} selected outpost");

                        Utils.SendReply(player, $"Teleporting to outpost in {configData.countdownSeconds} seconds");

                        AddTimer(player, configData.server.Locations["outpost"]);
                    }
                }
                break;
            case "bandit":
                {
                    if (configData.server.Locations["bandit"] != default && configData.server.Locations["bandit"] != null)
                    {
                        if (configData.debug) Utils.DoLog($"Player {player.displayName} selected bandit");

                        Utils.SendReply(player, $"Teleporting to bandit town in {configData.countdownSeconds} seconds");

                        AddTimer(player, configData.server.Locations["bandit"]);
                    }
                }
                break;
            case "sethome":
                {
                    playerHomes[player.userID].Locations.TryGetValue(args[1].ToString(), out Vector3 location);
                    if (location == default)
                    {
                        playerHomes[player.userID].Locations.Add(args[1].ToString(), player.transform.position);
                        SaveData();
                        Utils.SendReply(player, $"Added home {args[1]}");
                        return;
                    }
                    Utils.SendReply(player, $"Home {args[1]} already exists!");
                }
                break;
            case "removehome":
                {

                    playerHomes[player.userID].Locations.TryGetValue(args[1].ToString(), out Vector3 location);
                    if (location != default)
                    {
                        playerHomes[player.userID].Locations.Remove(args[1]);
                        SaveData();
                        Utils.SendReply(player, $"Removed home {args[1]}");
                    }
                }
                break;
            case "home":
                {
                    playerHomes[player.userID].Locations.TryGetValue(args[1].ToString(), out Vector3 location);
                    if (location != default && location != null)
                    {
                        Utils.SendReply(player, $"Teleporting to home {args[1]} in {configData.countdownSeconds} seconds");

                        AddTimer(player, location);
                        return;
                    }
                    Utils.SendReply(player, $"No such home {args[1]}");
                }
                break;
        }
    }

    private void AddTimer(BasePlayer player, Vector3 target)
    {
        playerTP.Add(player.userID, new TpTimer()
        {
            player = player,
            target = target,
            timer = new System.Timers.Timer
            {
                Interval = configData.countdownSeconds * 1000
            }
        });
        playerTP[player.userID].timer.Elapsed += TeleportCountdownElapsed;
        playerTP[player.userID].timer.Enabled = true;
    }

    private void TeleportCountdownElapsed(object source, System.Timers.ElapsedEventArgs e)
    {
        //KeyValuePair<ulong, TpTimer> ptp = playerTP.Where(x => x.Value.timer == source).ToList().First();
        foreach (var ptp in playerTP)
        {
            if (ptp.Value.timer == source)
            {
                Teleport(ptp.Value.player, ptp.Value.target);
                playerTP.Remove(ptp.Key);
                break;
            }
        }
    }
    private void Teleport(BasePlayer player, Vector3 target)
    {
        Utils.DoLog($"Teleporting {player.displayName} to {target}");
        if (player.net?.connection != null)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
        }

        player.SetParent(null, true, true);
        player.EnsureDismounted();
        player.Teleport(target);
        player.UpdateNetworkGroup();
        player.StartSleeping();
        player.SendNetworkUpdateImmediate(false);
        Utils.DoLog("Done!");
        // Kicked here with either one of these...
        //if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
        //player.EndSleeping();
    }

    public void FindMonuments()
    {
        if (configData.debug) Utils.DoLog("Looking for monuments...");
        foreach (MonumentInfo monument in Object.FindObjectsOfType<MonumentInfo>())
        {
            if (monument.name.Contains("compound"))
            {
                if (configData.debug) Utils.DoLog($"Found compound at {monument.transform.position}");
                configData.server.Locations["outpost"] = monument.transform.position;
                Vector3 mt = Vector3.zero;
                Vector3 bbq = Vector3.zero;
                foreach (Collider coll in Physics.OverlapSphere(monument.transform.position, 100, LayerMask.GetMask("Deployed")))
                {
                    BaseEntity entity = coll.gameObject.GetComponent<BaseEntity>();
                    if (entity == null) continue;
                    //if (configData.debug) Utils.DoLog($"Found entity: {entity.ShortPrefabName} {entity.PrefabName}");
                    if (entity.PrefabName.Contains("marketterminal") && mt == Vector3.zero)
                    {
                        if (configData.debug) Utils.DoLog($"Found marketterminal at compound at {entity.transform.position}");
                        mt = entity.transform.position;
                    }
                    else if (entity.PrefabName.Contains("bbq"))
                    {
                        if (configData.debug) Utils.DoLog($"Found bbq at compound at {entity.transform.position}");
                        bbq = entity.transform.position;
                    }
                }
                if (mt != Vector3.zero && bbq != Vector3.zero)
                {
                    if (configData.debug) Utils.DoLog($" Adding Outpost target at {configData.server.Locations["outpost"]}");
                    configData.server.Locations["outpost"] = Vector3.Lerp(mt, bbq, 0.3f);
                }
            }
            else if (monument.name.Contains("bandit"))
            {
                if (configData.debug) Utils.DoLog($"Found bandit at {monument.transform.position}");
                configData.server.Locations["bandit"] = monument.transform.position;
                foreach (Collider coll in Physics.OverlapSphere(monument.transform.position, 150, LayerMask.GetMask("Deployed")))
                {
                    BaseEntity entity = coll.gameObject.GetComponent<BaseEntity>();
                    if (entity == null) continue;
                    //if (configData.debug) Utils.DoLog($"Found entity: {entity.ShortPrefabName} {entity.PrefabName}");
                    if (entity.PrefabName.Contains("marketterminal"))
                    {
                        if (configData.debug) Utils.DoLog($"Found marketterminal at bandit at {entity.transform.position}");
                        configData.server.Locations["bandit"] = entity.transform.position + new Vector3(3f, 0.1f, 3f);
                    }
                }
                if (configData.debug) Utils.DoLog($" Adding BanditTown target at {configData.server.Locations["bandit"]}");
            }
        }
        SaveConfig(configData);
    }
}
