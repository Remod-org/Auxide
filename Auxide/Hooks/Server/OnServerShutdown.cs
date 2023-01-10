using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "Shutdown")]
    public static class OnServerShutdown
    {
        internal static TimeSince _lastcall;
        public static void Postfix()
        {
            if (Auxide.full)
            {
                if (_lastcall <= 0.5f)
                {
                    return;
                }
                Auxide.Scripts?.OnServerSaveHook();
                Auxide.Scripts?.OnServerShutdownHook();
                _lastcall = 0f;
            }
        }
    }
}
