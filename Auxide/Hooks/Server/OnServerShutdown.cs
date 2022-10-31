using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "Shutdown")]
    public class OnServerShutdown
    {
        internal static TimeSince _call;
        public static void Postfix()
        {
            if (Auxide.full)
            {
                if (_call <= 0.5f)
                {
                    return;
                }
                Auxide.Scripts?.OnServerSaveHook();
                Auxide.Scripts?.OnServerShutdownHook();
                _call = 0f;
            }
        }
    }
}
