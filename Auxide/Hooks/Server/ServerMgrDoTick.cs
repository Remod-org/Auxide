using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "DoTick")]
    public static class ServerMgrDoTick
    {
        [HarmonyPrefix]
        public static void Prefix(ServerMgr __instance)
        {
            if (Auxide.full)
            {
                Auxide.OnFrame(1);
                //Auxide.Scripts?.OnTickHook();
            }
        }
    }
}
