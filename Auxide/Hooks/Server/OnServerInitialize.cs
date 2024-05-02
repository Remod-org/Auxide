using HarmonyLib;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "Initialize")]
    public static class OnServerInitialize
    {
        internal static TimeSince _lastcall;
        public static void Prefix()
        {
            if (Auxide.full)
            {
                if (_lastcall <= 0.5f)
                {
                    return;
                }
                Auxide.Scripts?.OnServerInitializeHook();
                _lastcall = 0f;
            }
        }
    }
}
