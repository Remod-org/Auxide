using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Auxide
{
    public static class UlongExtension
    {
        public static bool IsSteamId(this string id)
        {
            if (!ulong.TryParse(id, out ulong num))
            {
                return false;
            }
            return num > 76561197960265728L;
        }

        public static bool IsSteamId(this ulong id)
        {
            return id > 76561197960265728L;
        }
    }

    public static class StringExtension
    {
        private readonly static Regex regexSplitQuotes;

        private static char[] spaceOrQuote;

        private static StringBuilder _quoteSafeBuilder;

        private static char[] FilenameDelim;

        private readonly static char[] _badCharacters;

        static StringExtension()
        {
            regexSplitQuotes = new Regex("\"([^\"]+)\"|'([^']+)'|\\S+");
            spaceOrQuote = new char[] { ' ', '\"' };
            _quoteSafeBuilder = new StringBuilder();
            FilenameDelim = new char[] { '/', '\\' };
            _badCharacters = new char[] { '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\a', '\b', '\t', '\v', '\f', '\r', '\u000E', '\u000F', '\u0010', '\u0012', '\u0013', '\u0014', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F', '\u00A0', '­', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A', '\u200B', '\u200C', '\u200D', '\u200E', '\u200F', '‐', '‑', '‒', '–', '—', '―', '‖', '‗', '‘', '’', '‚', '‛', '“', '”', '„', '‟', '\u2028', '\u2029', '\u202F', '\u205F', '\u2060', '\u2420', '\u2422', '\u2423', '\u3000', '\uFEFF' };
        }

        public static string TitleCase(this string text)
        {
            return CultureInfo.InstalledUICulture.TextInfo.ToTitleCase(text.Contains('\u005F') ? text.Replace('\u005F', ' ') : text);
        }

        public static string Titleize(this string text)
        {
            return text.TitleCase();
        }

        public static string[] SplitQuotesStrings(this string input, int maxCount = 2147483647)
        {
            input = input.Replace("\\\"", "&qute;");
            List<string> strs = new List<string>();
            Match match = regexSplitQuotes.Match(input);
            for (int i = 0; i < maxCount && match.Success; i++)
            {
                string str = match.Value.Trim(spaceOrQuote);
                strs.Add(str.Replace("&qute;", "\""));
                match = match.NextMatch();
            }
            return strs.ToArray();
        }
    }

    public static class DroppedItemContainerExtension
    {
        public static ItemContainer GetInventory(this object obj)
        {
            FieldInfo field = obj.GetType().GetField("inventory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (ItemContainer)field?.GetValue(obj);
        }

        public static void SetInventory(this object obj, ItemContainer val)
        {
            Type t = obj.GetType();
            if (t.GetProperty("inventory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
            {
                throw new ArgumentOutOfRangeException("inventory", string.Format("Property {0} was not found in Type {1}", "inventory", obj.GetType().FullName));
            }
            t.InvokeMember("inventory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
        }
    }

    public static class BaseNetworkableExtension
    {
        public static bool GetLimitedNetworking(this object obj)
        {
            FieldInfo field = obj.GetType().GetField("_limitedNetworking", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)field?.GetValue(obj);
        }

        public static void SetLimitedNetworking(this object obj, bool val)
        {
            Type t = obj.GetType();
            if (t.GetProperty("_limitedNetworking", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
            {
                throw new ArgumentOutOfRangeException("_limitedNetworking", string.Format("Property {0} was not found in Type {1}", "_limitedNetworking", obj.GetType().FullName));
            }
            t.InvokeMember("_limitedNetworking", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
        }
    }

    public static class CodeLockExtension
    {
        public static string GetCode(this object obj)
        {
            FieldInfo field = obj.GetType().GetField("code", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)field?.GetValue(obj);
        }

        public static void SetCode(this object obj, string val)
        {
            Type t = obj.GetType();
            if (t.GetProperty("code", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
            {
                throw new ArgumentOutOfRangeException("code", string.Format("Property {0} was not found in Type {1}", "code", obj.GetType().FullName));
            }
            t.InvokeMember("code", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
        }

        public static string GetGuestCode(this object obj)
        {
            FieldInfo field = obj.GetType().GetField("guestCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)field?.GetValue(obj);
        }

        public static void SetGuestCode(this object obj, string val)
        {
            Type t = obj.GetType();
            if (t.GetProperty("guestCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
            {
                throw new ArgumentOutOfRangeException("guestCode", string.Format("Property {0} was not found in Type {1}", "guestCode", obj.GetType().FullName));
            }
            t.InvokeMember("guestCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
        }
    }

    //public static class StorageContainerExtension
    //{
    //    public static bool GetLimitedNetworking(this object obj)
    //    {
    //        FieldInfo field = obj.GetType().GetField("_limitedNetworking", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    //        return (bool)field?.GetValue(obj);
    //    }

    //    public static void SetLimitedNetworking(this object obj, bool val)
    //    {
    //        Type t = obj.GetType();
    //        if (t.GetProperty("_limitedNetworking", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
    //        {
    //            throw new ArgumentOutOfRangeException("_limitedNetworking", string.Format("Property {0} was not found in Type {1}", "_limitedNetworking", obj.GetType().FullName));
    //        }
    //        t.InvokeMember("_limitedNetworking", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
    //    }
    //}
}