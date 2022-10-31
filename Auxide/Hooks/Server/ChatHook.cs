using ConVar;
using Harmony;
using UnityEngine;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(Chat), "sayImpl", new[] { typeof(Chat.ChatChannel), typeof(ConsoleSystem.Arg) })]
    public class ChatHook
    {
        // This patch links to a hook called OnChatCommand()
        //private static readonly bool verbose = Auxide.config["VerboseLogging"] != null && Convert.ToBoolean(Auxide.config["VerboseLogging"].Value);

        //static MethodBase TargetMethod()
        //{
        //    return AccessTools.Method(typeof(Chat), "sayImpl", new[] { typeof(Chat.ChatChannel), typeof(ConsoleSystem.Arg) });
        //}
        public static bool Prefix(Chat __instance, ref Chat.ChatChannel targetChannel, ref ConsoleSystem.Arg arg)
        {
            string str = arg.GetString(0, "text");
            if (str != null)
            {
                if (Auxide.verbose) arg.ReplyWith($"I heard you say '{str}' in chat!");
                Auxide.Scripts?.OnChatCommandHook(arg.Player(), str, arg.Args);
            }
            return true;
        }
    }
}
