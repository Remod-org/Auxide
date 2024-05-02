using HarmonyLib;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "OpenConnection")]
    public static class OnServerInitialized
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
                Auxide.Scripts?.OnServerInitializedHook();
                _lastcall = 0f;
            }
        }
    }
}
