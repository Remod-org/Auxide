using HarmonyLib;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseGameMode), "OnPlayerDisconnected", typeof(BasePlayer))]
    public static class PlayerDisconnect
    {
        public static void Postfix(BaseGameMode __instance, ref BasePlayer player)
        {
            if (!Auxide.full) return;
            Auxide.Scripts?.OnPlayerLeaveHook(player);
        }
    }
}
