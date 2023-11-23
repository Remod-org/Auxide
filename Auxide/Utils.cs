using Facepunch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Auxide
{
    public class Interface
    {
        internal bool playerTakingDamage;
        public object CallHook(string hook)
        {
            return Auxide.Scripts.BroadcastReturn(hook);
        }

        public object CallHook(string hook, object arg1)
        {
            return Auxide.Scripts.BroadcastReturn(hook, arg1);
        }

        public object CallHook(string hook, object arg1, object arg2)
        {
            if (hook == "OnTakeDamage" && arg1 is BasePlayer)
            {
                if (playerTakingDamage)
                {
                    playerTakingDamage = false;
                    return null;
                }
                playerTakingDamage = true;
            }
            return Auxide.Scripts.BroadcastReturn(hook, arg1, arg2);
        }

        public object CallHook(string hook, object arg1, object arg2, object arg3)
        {
            return Auxide.Scripts.BroadcastReturn(hook, arg1, arg2, arg3);
        }

        public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4)
        {
            return Auxide.Scripts.BroadcastReturn(hook, arg1, arg2, arg3, arg4);
        }

        public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return Auxide.Scripts.BroadcastReturn(hook, arg1, arg2, arg3, arg4, arg5);
        }
    }

    public sealed class Utils : Auxide
    {
        public static bool IsFriend(ulong playerid, ulong ownerid)
        {
            if (playerid == 0) return true;
            if (ownerid == 0) return true;

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
            string now = DateTime.Now.ToString("yyyyMMdd");
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

        public static void DoLog(string tolog)
        {
            DoLog(tolog, false, false);
        }

        public static void DoLog(string tolog, bool logCaller = true, bool warning = false)
        {
            string caller = logCaller ? GetCaller() : "Auxide";
            string warn = warning ? "WARNING " : "";
            string now = DateTime.Now.ToShortTimeString();

            //using (FileStream fs = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            using (FileStream fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"[{warn}{caller} ({now})] {tolog}");
                }
            }
        }

        public static void LogToFile(string filename, string text, RustScript plugin, bool timeStamp = true)
        {
            string str = Path.Combine(LogPath, plugin.Name);
            if (!Directory.Exists(str))
            {
                Directory.CreateDirectory(str);
            }
            string[] lower = new string[] { plugin.Name.ToLower(), "_", filename.ToLower(), null, null };
            lower[3] = (timeStamp ? string.Format("-{0:yyyy-MM-dd}", DateTime.Now) : "");
            lower[4] = ".txt";
            filename = string.Concat(lower);
            using (StreamWriter streamWriter = new StreamWriter(Path.Combine(str, filename), true))
            {
                streamWriter.WriteLine(timeStamp ? string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, text) : text);
            }
        }

        private static string GetCaller(int level = 2)
        {
            MethodBase m = new StackTrace()?.GetFrame(level)?.GetMethod();
            return m?.DeclaringType?.FullName ?? "";
        }

        public static bool GetBoolValue(string value)
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

        public static object OnRunCommandLine()
        {
            foreach (KeyValuePair<string, string> @switch in CommandLine.GetSwitches())
            {
                string value = @switch.Value;
                if (value?.Length == 0)
                {
                    value = "1";
                }
                string str = @switch.Key.Substring(1);
                ConsoleSystem.Option unrestricted = ConsoleSystem.Option.Unrestricted;
                unrestricted.PrintOutput = false;
                if (Scripts?.OnConsoleCommandHook(str, new object[] { value }) == null)
                {
                    return null;
                }
                ConsoleSystem.Run(unrestricted, str, new object[] { value });
            }
            return false;
        }

        public static class ArrayPool
        {
            private const int MaxArrayLength = 50;
            private const int InitialPoolAmount = 64;
            private const int MaxPoolAmount = 256;
            private static List<Queue<object[]>> _pooledArrays = new List<Queue<object[]>>();

            static ArrayPool()
            {
                for (int i = 0; i < 50; i++)
                {
                    _pooledArrays.Add(new Queue<object[]>());
                    SetupArrays(i + 1);
                }
            }

            public static object[] Get(int length)
            {
                if (length == 0 || length > 50)
                {
                    return new object[length];
                }
                Queue<object[]> queue = _pooledArrays[length - 1];
                Queue<object[]> obj = queue;
                object[] result;
                lock (obj)
                {
                    if (queue.Count == 0)
                    {
                        SetupArrays(length);
                    }
                    result = queue.Dequeue();
                }
                return result;
            }

            public static void Free(object[] array)
            {
                if (array == null || array.Length == 0 || array.Length > 50)
                {
                    return;
                }
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = null;
                }
                Queue<object[]> queue = _pooledArrays[array.Length - 1];
                Queue<object[]> obj = queue;
                lock (obj)
                {
                    if (queue.Count > 256)
                    {
                        for (int j = 0; j < 64; j++)
                        {
                            queue.Dequeue();
                        }
                    }
                    else
                    {
                        queue.Enqueue(array);
                    }
                }
            }

            private static void SetupArrays(int length)
            {
                Queue<object[]> queue = _pooledArrays[length - 1];
                for (int i = 0; i < 64; i++)
                {
                    queue.Enqueue(new object[length]);
                }
            }
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