using Auxide;
using Harmony;
using Network;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class HTeleport : RustScript
{
    private static readonly bool debug = true;
    private static readonly float countdownSeconds = 5;
    private static Vector3 outpost;
    private static Vector3 bandit;
    private static Dictionary<ulong, TpTimer> playerTP = new Dictionary<ulong, TpTimer>();
    private static Dictionary<ulong, HomeData> playerHomes = new Dictionary<ulong, HomeData>();

    private class HomeData
    {
        [JsonProperty("l")]
        public Dictionary<string, Vector3> Locations { get; set; } = new Dictionary<string, Vector3>();

        [JsonProperty("t")]
        public TeleportData Teleports { get; set; } = new TeleportData();
    }

    private class TeleportData
    {
        [JsonProperty("a")]
        public int Amount { get; set; }

        [JsonProperty("d")]
        public string Date { get; set; }

        [JsonProperty("t")]
        public int Timestamp { get; set; }
    }

    public override void Initialize()
    {
        FindMonuments();
    }

    public class TpTimer
    {
        public BasePlayer player;
        public Vector3 target;
        public System.Timers.Timer timer;
    }

    public void LoadData()
    {
        playerHomes = data.ReadObject<Dictionary<ulong,HomeData>>(Name);
    }

    public void SaveData()
    {
        data.WriteObject(Name, playerHomes);
    }

    public void OnChatCommand(BasePlayer player, string chat, object[] args = null)
    {
        string arginfo = string.Join(",", args);
        Utils.DoLog($"Heard: {chat}/{arginfo}");
        Connection connection = player.net.connection;
        object[] objArray = new object[] { 2, 0, null };
        if (chat == "/outpost" && outpost != default(Vector3))
        {
            if (debug) Utils.DoLog($"Player {player.displayName} selected /outpost");

            objArray[2] = "Teleporting to outpost in 5 seconds";
            ConsoleNetwork.SendClientCommand(connection, "chat.add", objArray);

            AddTimer(player, outpost);
        }
        else if (chat == "/bandit" && bandit != default(Vector3))
        {
            if (debug) Utils.DoLog($"Player {player.displayName} selected /bandit");

            objArray[2] = "Teleporting to bandit town in 5 seconds";
            ConsoleNetwork.SendClientCommand(connection, "chat.add", objArray);

            AddTimer(player, bandit);
        }
        else if (chat.Contains("/sethome"))
        {
            if (!playerHomes.ContainsKey(player.userID))
            {
                playerHomes.Add(player.userID, new HomeData());
            }

            playerHomes[player.userID].Locations.TryGetValue(args[0].ToString(), out Vector3 location);
            if (location == default)
            {
                playerHomes[player.userID].Locations.Add(args[0].ToString(), player.transform.position);
                objArray[2] = $"Added home {args[0]}";
                ConsoleNetwork.SendClientCommand(connection, "chat.add", objArray);
                return;
            }
            objArray[2] = $"Home {args[0]} already exists!";
            ConsoleNetwork.SendClientCommand(connection, "chat.add", objArray);
        }
        else if (chat.Contains("/home"))
        {
            if (!playerHomes.ContainsKey(player.userID))
            {
                playerHomes.Add(player.userID, new HomeData());
            }

            playerHomes[player.userID].Locations.TryGetValue(args[0].ToString(), out Vector3 location);
            if (location != default)
            {
                objArray[2] = $"Teleporting to home {args[0]} in 5 seconds";
                ConsoleNetwork.SendClientCommand(connection, "chat.add", objArray);

                AddTimer(player, location);
            }
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
                Interval = countdownSeconds * 1000
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

    public static void FindMonuments()
    {
        if (debug) Utils.DoLog("Looking for monuments...");
        foreach (MonumentInfo monument in Object.FindObjectsOfType<MonumentInfo>())
        {
            if (monument.name.Contains("compound"))
            {
                if (debug) Utils.DoLog($"Found compound at {monument.transform.position}");
                outpost = monument.transform.position;
                Vector3 mt = Vector3.zero;
                Vector3 bbq = Vector3.zero;
                foreach (Collider coll in Physics.OverlapSphere(monument.transform.position, 100, LayerMask.GetMask("Deployed")))
                {
                    BaseEntity entity = coll.gameObject.GetComponent<BaseEntity>();
                    if (entity == null) continue;
                    //if (debug) Utils.DoLog($"Found entity: {entity.ShortPrefabName} {entity.PrefabName}");
                    if (entity.PrefabName.Contains("marketterminal") && mt == Vector3.zero)
                    {
                        if (debug) Utils.DoLog($"Found marketterminal at compound at {entity.transform.position}");
                        mt = entity.transform.position;
                    }
                    else if (entity.PrefabName.Contains("bbq"))
                    {
                        if (debug) Utils.DoLog($"Found bbq at compound at {entity.transform.position}");
                        bbq = entity.transform.position;
                    }
                }
                if (mt != Vector3.zero && bbq != Vector3.zero)
                {
                    if (debug) Utils.DoLog($" Adding Outpost target at {outpost}");
                    outpost = Vector3.Lerp(mt, bbq, 0.3f);
                }
            }
            else if (monument.name.Contains("bandit"))
            {
                if (debug) Utils.DoLog($"Found bandit at {monument.transform.position}");
                bandit = monument.transform.position;
                foreach (Collider coll in Physics.OverlapSphere(monument.transform.position, 150, LayerMask.GetMask("Deployed")))
                {
                    BaseEntity entity = coll.gameObject.GetComponent<BaseEntity>();
                    if (entity == null) continue;
                    //if (debug) Utils.DoLog($"Found entity: {entity.ShortPrefabName} {entity.PrefabName}");
                    if (entity.PrefabName.Contains("marketterminal"))
                    {
                        if (debug) Utils.DoLog($"Found marketterminal at bandit at {entity.transform.position}");
                        bandit = entity.transform.position + new Vector3(3f, 0.1f, 3f);
                    }
                }
                if (debug) Utils.DoLog($" Adding BanditTown target at {bandit}");
            }
        }
    }
}
