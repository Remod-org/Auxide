using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Auxide
{
    public sealed class Utils : Auxide
    {
        public static bool IsFriend(ulong playerid, ulong ownerid)
        {
            if (playerid == 0) return true;

            DoLog($"IsFriend player {playerid} owner {ownerid}");
            BasePlayer player = BasePlayer.FindByID(playerid);
            if (player != null && player?.currentTeam != 0)
            {
                RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (playerTeam?.members.Contains(ownerid) == true)
                {
                    return true;
                }
            }
            return false;
        }

        public static void GetNewLog()
        {
            //string now = DateTime.Now.ToString("yyyyMdd-HH:mm");
            string now = DateTime.Now.ToString("yyyyMdd");
            LogFile = Path.Combine(LogPath, $"auxide_{now}.log");
        }

        public static void TruncateLog()
        {
            if (File.Exists(LogFile))
            {
                using (FileStream fs = new FileStream(LogPath, FileMode.Truncate))
                {
                    fs.SetLength(0);
                }
            }
        }

        public static void SendReply(BasePlayer player, string text)
        {
            object[] objArray = new object[] { 2, 0, text };
            ConsoleNetwork.SendClientCommand(player.net.connection, "chat.add", objArray);
        }

        public static void DoLog(string tolog, bool logCaller = true)
        {
            string caller = logCaller ? GetCaller() : "Auxide";
            string now = DateTime.Now.ToShortTimeString();

            //using (FileStream fs = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            using (FileStream fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"[{caller} ({now})] {tolog}");
                }
            }
        }

        private static string GetCaller(int level = 2)
        {
            MethodBase m = new StackTrace().GetFrame(level).GetMethod();
            return m?.DeclaringType?.FullName;
        }

        private static bool GetBoolValue(string value)
        {
            if (value == null) return false;
            value = value.Trim().ToLower();
            switch (value)
            {
                case "on":
                case "true":
                case "yes":
                case "1":
                case "t":
                case "y":
                    return true;
                default:
                    return false;
            }
        }

        //public static void DatafileToProto<T>(string name, bool deleteAfter = true)
        //{
        //    DataFileSystem dataFileSystem = DataFileSystem;
        //    if (!dataFileSystem.ExistsDatafile(name))
        //    {
        //        return;
        //    }
        //    if (ProtoStorage.Exists(new string[] { name }))
        //    {
        //        Interface.Oxide.LogWarning("Failed to import JSON file: {0} already exists.", new object[] { name });
        //        return;
        //    }
        //    try
        //    {
        //        ProtoStorage.Save<T>(dataFileSystem.ReadObject<T>(name), new string[] { name });
        //        if (deleteAfter)
        //        {
        //            File.Delete(dataFileSystem.GetFile(name).Filename);
        //        }
        //    }
        //    catch (Exception exception1)
        //    {
        //        Exception exception = exception1;
        //        DoLog(string.Concat("Failed to convert datafile to proto storage: ", name, exception));
        //    }
        //}
    }
}
public static class StringExtension
{
    public static string Titleize(this string s)
    {
        bool IsNewSentence = true;
        StringBuilder result = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (IsNewSentence && char.IsLetter(s[i]))
            {
                result.Append(char.ToUpper(s[i]));
                IsNewSentence = false;
            }
            else
            {
                result.Append(s[i]);
            }

            if (s[i] == '!' || s[i] == '?' || s[i] == '.')
            {
                IsNewSentence = true;
            }
        }

        return result.ToString();
    }
}
