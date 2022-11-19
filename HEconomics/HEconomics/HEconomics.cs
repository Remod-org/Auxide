using Auxide;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Auxide.Scripting;
using Facepunch.Extend;

// TODO: Add SQLite and MySQL database support

public class Economics : RustScript
{
    #region Configuration
    private Configuration configData;
    private readonly ScriptManager sm;

    private class Configuration
    {
        [JsonProperty("Allow negative balance for accounts")]
        public bool AllowNegativeBalance = false;

        [JsonProperty("Balance limit for accounts (0 to disable)")]
        public int BalanceLimit = 0;

        [JsonProperty("Maximum balance for accounts (0 to disable)")] // TODO: From version 3.8.6; remove eventually
        private int BalanceLimitOld { set { BalanceLimit = value; } }

        [JsonProperty("Negative balance limit for accounts (0 to disable)")]
        public int NegativeBalanceLimit = 0;

        [JsonProperty("Remove unused accounts")]
        public bool RemoveUnused = true;

        [JsonProperty("Log transactions to file")]
        public bool LogTransactions = false;

        [JsonProperty("Starting account balance (0 or higher)")]
        public int StartingBalance = 1000;

        [JsonProperty("Starting money amount (0 or higher)")] // TODO: From version 3.8.6; remove eventually
        private int StartingBalanceOld { set { StartingBalance = value; } }

        [JsonProperty("Wipe balances on new save file")]
        public bool WipeOnNewSave = false;

        public string ToJson() => JsonConvert.SerializeObject(this);

        public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
    }
    public string Lang(string input, params object[] args)
    {
        return string.Format(lang.Get(input), args);
    }

    public void Message(BasePlayer player, string input, params object[] args)
    {
        Utils.SendReply(player, string.Format(lang.Get(input), args));
    }


    public void LoadDefaultConfig() => configData = new Configuration();

    public void LoadConfig()
    {
        try
        {
            configData = configData.ReadObject<Configuration>();
            if (config == null)
            {
                throw new JsonException();
            }

            if (!configData.ToDictionary().Keys.SequenceEqual(configData.ToDictionary(x => x.Key, x => x.Value).Keys))
            {
                Utils.DoLog("Configuration appears to be outdated; updating and saving");
                SaveConfig();
            }
        }
        catch
        {
            Utils.DoLog($"Configuration file {Name}.json is invalid; using defaults");
            LoadDefaultConfig();
        }
    }

    public void SaveConfig()
    {
        Utils.DoLog($"Configuration changes saved to {Name}.json");
        configData.WriteObject(configData, true);
    }

    #endregion Configuration

    #region Stored Data

    //private DynamicConfigFile data;
    private StoredData storedData;
    private bool changed;

    private class StoredData
    {
        public readonly Dictionary<string, double> Balances = new Dictionary<string, double>();
    }

    private void SaveData()
    {
        if (changed)
        {
            Utils.DoLog("Saving balances for players...");
            data.WriteObject(Name, storedData);
        }
    }

    private void OnServerSave() => SaveData();

    private void Unload() => SaveData();

    #endregion Stored Data

    #region Localization

    public override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["CommandBalance"] = "balance",
            ["CommandDeposit"] = "deposit",
            ["CommandSetBalance"] = "SetBalance",
            ["CommandTransfer"] = "transfer",
            ["CommandWithdraw"] = "withdraw",
            ["CommandWipe"] = "ecowipe",
            ["DataSaved"] = "Economics data saved!",
            ["DataWiped"] = "Economics data wiped!",
            ["DepositedToAll"] = "Deposited {0:C} total ({1:C} each) to {2} player(s)",
            ["LogDeposit"] = "{0:C} deposited to {1}",
            ["LogSetBalance"] = "{0:C} set as balance for {1}",
            ["LogTransfer"] = "{0:C} transferred to {1} from {2}",
            ["LogWithdrawl"] = "{0:C} withdrawn from {1}",
            ["NegativeBalance"] = "Balance can not be negative!",
            ["NotAllowed"] = "You are not allowed to use the '{0}' command",
            ["NoPlayersFound"] = "No players found with name or ID '{0}'",
            ["PlayerBalance"] = "Balance for {0}: {1:C}",
            ["PlayerLacksMoney"] = "'{0}' does not have enough money!",
            ["PlayersFound"] = "Multiple players were found, please specify: {0}",
            ["ReceivedFrom"] = "You have received {0} from {1}",
            ["SetBalanceForAll"] = "Balance set to {0:C} for {1} player(s)",
            ["TransactionFailed"] = "Transaction failed! Make sure amount is above 0",
            ["TransferredTo"] = "{0} transferred to {1}",
            ["TransferredToAll"] = "Transferred {0:C} total ({1:C} each) to {2} player(s)",
            ["TransferToSelf"] = "You can not transfer money yourself!",
            ["UsageBalance"] = "{0} - check your balance",
            ["UsageBalanceOthers"] = "{0} <player name or id> - check balance of a player",
            ["UsageDeposit"] = "{0} <player name or id> <amount> - deposit amount to player",
            ["UsageSetBalance"] = "Usage: {0} <player name or id> <amount> - set balance for player",
            ["UsageTransfer"] = "Usage: {0} <player name or id> <amount> - transfer money to player",
            ["UsageWithdraw"] = "Usage: {0} <player name or id> <amount> - withdraw money from player",
            ["UsageWipe"] = "Usage: {0} - wipe all economics data",
            ["YouLackMoney"] = "You do not have enough money!",
            ["YouLostMoney"] = "You lost: {0:C}",
            ["YouReceivedMoney"] = "You received: {0:C}",
            ["YourBalance"] = "Your balance is: {0:C}",
            ["WithdrawnForAll"] = "Withdrew {0:C} total ({1:C} each) from {2} player(s)",
            ["ZeroAmount"] = "Amount cannot be zero"
        }, Name);
    }

    #endregion Localization

    #region Initialization

    private const string permissionBalance = "economics.balance";
    private const string permissionDeposit = "economics.deposit";
    private const string permissionDepositAll = "economics.depositall";
    private const string permissionSetBalance = "economics.setbalance";
    private const string permissionSetBalanceAll = "economics.setbalanceall";
    private const string permissionTransfer = "economics.transfer";
    private const string permissionTransferAll = "economics.transferall";
    private const string permissionWithdraw = "economics.withdraw";
    private const string permissionWithdrawAll = "economics.withdrawall";
    private const string permissionWipe = "economics.wipe";

    private void Init()
    {
        // Register universal chat/console commands
        AddLocalizedCommand(nameof(CommandBalance));
        AddLocalizedCommand(nameof(CommandDeposit));
        AddLocalizedCommand(nameof(CommandSetBalance));
        AddLocalizedCommand(nameof(CommandTransfer));
        AddLocalizedCommand(nameof(CommandWithdraw));
        AddLocalizedCommand(nameof(CommandWipe));

        // Register permissions for commands
        Permissions.RegisterPermission(Name, permissionBalance);
        Permissions.RegisterPermission(Name, permissionDeposit);
        Permissions.RegisterPermission(Name, permissionDepositAll);
        Permissions.RegisterPermission(Name, permissionSetBalance);
        Permissions.RegisterPermission(Name, permissionSetBalanceAll);
        Permissions.RegisterPermission(Name, permissionTransfer);
        Permissions.RegisterPermission(Name, permissionTransferAll);
        Permissions.RegisterPermission(Name, permissionWithdraw);
        Permissions.RegisterPermission(Name, permissionWithdrawAll);
        Permissions.RegisterPermission(Name, permissionWipe);

        // Load existing data and migrate old data format
        //data = Interface.Oxide.DataFileSystem.GetFile(Name);
        try
        {
            Dictionary<ulong, double> temp = data.ReadObject<Dictionary<ulong, double>>("economics");
            try
            {
                storedData = new StoredData();
                foreach (KeyValuePair<ulong, double> old in temp)
                {
                    if (!storedData.Balances.ContainsKey(old.Key.ToString()))
                    {
                        storedData.Balances.Add(old.Key.ToString(), old.Value);
                    }
                }
                changed = true;
            }
            catch
            {
                // Ignored
            }
        }
        catch
        {
            storedData = data.ReadObject<StoredData>("economics");
            changed = true;
        }

        List<string> playerData = new List<string>(storedData.Balances.Keys);

        // Check for and set any balances over maximum allowed
        if (configData.BalanceLimit > 0)
        {
            foreach (string p in playerData)
            {
                if (storedData.Balances[p] > configData.BalanceLimit)
                {
                    storedData.Balances[p] = configData.BalanceLimit;
                    changed = true;
                }
            }
        }

        // Check for and remove any inactive player balance data
        if (configData.RemoveUnused)
        {
            foreach (string p in playerData)
            {
                if (storedData.Balances[p].Equals(configData.StartingBalance))
                {
                    storedData.Balances.Remove(p);
                    changed = true;
                }
            }
        }
    }

    private void OnNewSave()
    {
        if (configData.WipeOnNewSave)
        {
            storedData.Balances.Clear();
            changed = true;
            sm.Broadcast("OnEconomicsDataWiped");
        }
    }

    #endregion Initialization

    #region API Methods

    private double Balance(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Utils.DoLog("Balance method called without a valid player ID");
            return 0.0;
        }

        double playerData;
        return storedData.Balances.TryGetValue(playerId, out playerData) ? playerData : configData.StartingBalance;
    }

    private double Balance(ulong playerId) => Balance(playerId.ToString());

    private bool Deposit(string playerId, double amount)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Utils.DoLog("Deposit method called without a valid player ID");
            return false;
        }

        if (amount > 0 && SetBalance(playerId, amount + Balance(playerId)))
        {
            sm.Broadcast("OnEconomicsDeposit", playerId, amount);

            if (configData.LogTransactions)
            {
                //LogToFile("transactions", $"[{DateTime.Now}] {Lang("LogDeposit", null, amount, playerId)}", this);
            }

            return true;
        }

        return false;
    }

    private bool Deposit(ulong playerId, double amount) => Deposit(playerId.ToString(), amount);

    private bool SetBalance(string playerId, double amount)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Utils.DoLog("SetBalance method called without a valid player ID");
            return false;
        }

        if (amount >= 0 || configData.AllowNegativeBalance)
        {
            amount = Math.Round(amount, 2);
            if (configData.BalanceLimit > 0 && amount > configData.BalanceLimit)
            {
                amount = configData.BalanceLimit;
            }
            else if (configData.AllowNegativeBalance && configData.NegativeBalanceLimit < 0 && amount < configData.NegativeBalanceLimit)
            {
                amount = configData.NegativeBalanceLimit;
            }

            storedData.Balances[playerId] = amount;
            changed = true;

            sm.Broadcast("OnEconomicsBalanceUpdated", playerId, amount);
            sm.Broadcast("OnBalanceChanged", "OnEconomicsBalanceUpdated", new System.DateTime(2022, 7, 1), playerId, amount);

            if (configData.LogTransactions)
            {
                //LogToFile("transactions", $"[{DateTime.Now}] {Lang("LogSetBalance", null, amount, playerId)}", this);
            }

            return true;
        }

        return false;
    }

    private bool SetBalance(ulong playerId, double amount) => SetBalance(playerId.ToString(), amount);

    private bool Transfer(string playerId, string targetId, double amount)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Utils.DoLog("Transfer method called without a valid player ID");
            return false;
        }

        if (Withdraw(playerId, amount) && Deposit(targetId, amount))
        {
            sm.Broadcast("OnEconomicsTransfer", playerId, targetId, amount);

            if (configData.LogTransactions)
            {
                //LogToFile("transactions", $"[{DateTime.Now}] {Lang("LogTransfer", null, amount, targetId, playerId)}", this);
            }

            return true;
        }

        return false;
    }

    private bool Transfer(ulong playerId, ulong targetId, double amount)
    {
        return Transfer(playerId.ToString(), targetId.ToString(), amount);
    }

    private bool Withdraw(string playerId, double amount)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Utils.DoLog("Withdraw method called without a valid player ID");
            return false;
        }

        if (amount >= 0 || configData.AllowNegativeBalance)
        {
            double balance = Balance(playerId);
            if ((balance >= amount || (configData.AllowNegativeBalance && balance + amount > configData.NegativeBalanceLimit)) && SetBalance(playerId, balance - amount))
            {
                sm.Broadcast("OnEconomicsWithdrawl", playerId, amount);

                if (configData.LogTransactions)
                {
                    //LogToFile("transactions", $"[{DateTime.Now}] {Lang("LogWithdrawl", null, amount, playerId)}", this);
                }

                return true;
            }
        }

        return false;
    }

    private bool Withdraw(ulong playerId, double amount) => Withdraw(playerId.ToString(), amount);

    #endregion API Methods

    #region Commands

    #region Balance Command

    private void CommandBalance(IPlayer player, string command, string[] args)
    {
        if (args != null && args.Length > 0)
        {
            if (!player.HasPermission(permissionBalance))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            IPlayer target = FindPlayer(args[0], player);
            if (target == null)
            {
                Message(player, "UsageBalance", command);
                return;
            }

            Message(player, "PlayerBalance", target.Name, Balance(target.Id));
            return;
        }

        if (player.IsServer)
        {
            Message(player, "UsageBalanceOthers", command);
        }
        else
        {
            Message(player, "YourBalance", Balance(player.Id));
        }
    }

    #endregion Balance Command

    #region Deposit Command

    private void CommandDeposit(IPlayer player, string command, string[] args)
    {
        if (!player.HasPermission(permissionDeposit))
        {
            Message(player, "NotAllowed", command);
            return;
        }

        if (args == null || args.Length <= 1)
        {
            Message(player, "UsageDeposit", command);
            return;
        }

        double amount;
        double.TryParse(args[1], out amount);
        if (amount <= 0)
        {
            Message(player, "ZeroAmount");
            return;
        }

        if (args[0] == "*")
        {
            if (!player.HasPermission(permissionDepositAll))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            int receivers = 0;
            foreach (string targetId in storedData.Balances.Keys.ToList())
            {
                if (Deposit(targetId, amount))
                {
                    receivers++;
                }
            }
            Message(player, "DepositedToAll", amount * receivers, amount, receivers);
        }
        else
        {
            IPlayer target = FindPlayer(args[0], player);
            if (target == null)
            {
                return;
            }

            if (Deposit(target.Id, amount))
            {
                Message(player, "PlayerBalance", target.Name, Balance(target.Id));
            }
            else
            {
                Message(player, "TransactionFailed", target.Name);
            }
        }
    }

    #endregion Deposit Command

    #region Set Balance Command

    private void CommandSetBalance(IPlayer player, string command, string[] args)
    {
        if (!player.HasPermission(permissionSetBalance))
        {
            Message(player, "NotAllowed", command);
            return;
        }

        if (args == null || args.Length <= 1)
        {
            Message(player, "UsageSetBalance", command);
            return;
        }

        double amount;
        double.TryParse(args[1], out amount);

        if (amount < 0)
        {
            Message(player, "NegativeBalance");
            return;
        }

        if (args[0] == "*")
        {
            if (!player.HasPermission(permissionSetBalanceAll))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            int receivers = 0;
            foreach (string targetId in storedData.Balances.Keys.ToList())
            {
                if (SetBalance(targetId, amount))
                {
                    receivers++;
                }
            }
            Message(player, "SetBalanceForAll", amount, receivers);
        }
        else
        {
            IPlayer target = FindPlayer(args[0], player);
            if (target == null)
            {
                return;
            }

            if (SetBalance(target.Id, amount))
            {
                Message(player, "PlayerBalance", target.Name, Balance(target.Id));
            }
            else
            {
                Message(player, "TransactionFailed", target.Name);
            }
        }
    }

    #endregion Set Balance Command

    #region Transfer Command

    private void CommandTransfer(IPlayer player, string command, string[] args)
    {
        if (!player.HasPermission(permissionTransfer))
        {
            Message(player, "NotAllowed", command);
            return;
        }

        if (args == null || args.Length <= 1)
        {
            Message(player, "UsageTransfer", command);
            return;
        }

        double amount;
        double.TryParse(args[1], out amount);

        if (amount <= 0)
        {
            Message(player, "ZeroAmount");
            return;
        }

        if (args[0] == "*")
        {
            if (!player.HasPermission(permissionTransferAll))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            if (!Withdraw(player.Id, amount))
            {
                Message(player, "YouLackMoney");
                return;
            }

            int receivers = BasePlayer.activePlayerList.Count();
            double splitAmount = amount /= receivers;

            foreach (BasePlayer target in BasePlayer.activePlayerList)
            {
                if (Deposit(target.UserIDString, splitAmount))
                {
                    if (target.IsConnected)
                    {
                        Message(target, "ReceivedFrom", splitAmount, player.Name);
                    }
                }
            }
            Message(player, "TransferedToAll", amount, splitAmount, receivers);
        }
        else
        {
            BasePlayer target = FindPlayer(args[0], player);
            if (target == null)
            {
                return;
            }

            if (target.Equals(player))
            {
                Message(player, "TransferToSelf");
                return;
            }

            if (!Withdraw(player.Id, amount))
            {
                Message(player, "YouLackMoney");
                return;
            }

            if (Deposit(target.UserIDString, amount))
            {
                Message(player, "TransferredTo", amount, target.displayName);
                Message(target, "ReceivedFrom", amount, player.Name);
            }
            else
            {
                Message(player, "TransactionFailed", target.displayName);
            }
        }
    }

    #endregion Transfer Command

    #region Withdraw Command

    private void CommandWithdraw(IPlayer player, string command, string[] args)
    {
        if (!player.HasPermission(permissionWithdraw))
        {
            Message(player, "NotAllowed", command);
            return;
        }

        if (args == null || args.Length <= 1)
        {
            Message(player, "UsageWithdraw", command);
            return;
        }

        double amount;
        double.TryParse(args[1], out amount);

        if (amount <= 0)
        {
            Message(player, "ZeroAmount");
            return;
        }

        if (args[0] == "*")
        {
            if (!player.HasPermission(permissionWithdrawAll))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            int receivers = 0;
            foreach (string targetId in storedData.Balances.Keys.ToList())
            {
                if (Withdraw(targetId, amount))
                {
                    receivers++;
                }
            }
            Message(player, "WithdrawnForAll", amount * receivers, amount, receivers);
        }
        else
        {
            IPlayer target = FindPlayer(args[0], player);
            if (target == null)
            {
                return;
            }

            if (Withdraw(target.Id, amount))
            {
                Message(player, "PlayerBalance", target.Name, Balance(target.Id));
            }
            else
            {
                Message(player, "YouLackMoney", target.Name);
            }
        }
    }

    #endregion Withdraw Command

    #region Wipe Command

    private void CommandWipe(IPlayer player, string command, string[] args)
    {
        if (!player.HasPermission(permissionWipe))
        {
            Message(player, "NotAllowed", command);
            return;
        }

        storedData = new StoredData();
        changed = true;
        SaveData();

        Message(player, "DataWiped");
        sm.Broadcast("OnEconomicsDataWiped", player);
    }

    #endregion Wipe Command

    #endregion Commands

    #region Helpers

    private BasePlayer FindPlayer(string playerNameOrId, BasePlayer player)
    {
        BasePlayer[] foundPlayers = BasePlayer.allPlayerList.ToArray();// (playerNameOrId).ToArray();
        if (foundPlayers.Length > 1)
        {
            Message(player, "PlayersFound", string.Join(", ", foundPlayers.Select(p => p.displayName).Take(10).ToArray()).Truncate(60));
            return null;
        }

        BasePlayer target = foundPlayers.Length == 1 ? foundPlayers[0] : null;
        if (target == null)
        {
            Message(player, "NoPlayersFound", playerNameOrId);
            return null;
        }

        return target;
    }

    private void AddLocalizedCommand(string command)
    {
        foreach (string language in lang.GetLanguages(this))
        {
            Dictionary<string, string> messages = lang.GetMessages(language, this);
            foreach (KeyValuePair<string, string> message in messages)
            {
                if (message.Key.Equals(command))
                {
                    if (!string.IsNullOrEmpty(message.Value))
                    {
                        AddCovalenceCommand(message.Value, command);
                    }
                }
            }
        }
    }

    #endregion Helpers
}

#region Extension Methods

namespace Oxide.Plugins.EconomicsExtensionMethods
{
    public static class ExtensionMethods
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
            {
                return min;
            }
            else if (val.CompareTo(max) > 0)
            {
                return max;
            }
            else
            {
                return val;
            }
        }
    }
}

#endregion Extension Methods
