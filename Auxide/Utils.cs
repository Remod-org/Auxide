﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
            string now = DateTime.Now.ToString("yyyyMdd-HH:mm");
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

        public static void DoLog(string tolog, bool logCaller = true)
        {
            string caller = logCaller ? GetCaller() : "Auxide";
            string now = DateTime.Now.ToShortTimeString();

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
