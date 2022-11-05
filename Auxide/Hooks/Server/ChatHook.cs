using ConVar;
using Harmony;
using UnityEngine;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(Chat), "sayImpl", new[] { typeof(Chat.ChatChannel), typeof(ConsoleSystem.Arg) })]
    public class ChatHook
    {
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
