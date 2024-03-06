using ConVar;
using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(Chat), "Broadcast")]
    public static class ServerBroadcast
    {
        public static bool Prefix(ref string message, ref string username, ref string color, ref ulong userid)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.OnServerMessageHook(message, username, color, userid);
                if (res is bool)
                {
                    return false;
                }
            }
            else if (Auxide.hideGiveNotices)
            {
                return false;
            }
            return true;
        }
    }
}
