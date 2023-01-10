using Auxide;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Auxide
{
    public static class Interface
    {
        public static object CallHook(string hook)
        {
            return Auxide.Scripts.BroadcastReturn(hook);
        }

        public static object CallHook(string hook, object arg1)
        {
            return Auxide.Scripts.BroadcastReturn(hook, arg1);
        }

        public static object CallHook(string hook, object arg1, object arg2)
        {
            return Auxide.Scripts.BroadcastReturn(hook, arg1, arg2);
        }
    }

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