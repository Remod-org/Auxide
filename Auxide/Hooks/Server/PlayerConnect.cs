using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseGameMode), "OnPlayerConnected", typeof(BasePlayer))]
    public class PlayerConnect
    {
        public static void Postfix(BaseGameMode __instance, ref BasePlayer player)
        {
            if (!Auxide.full) return;
            Auxide.Scripts?.OnPlayerJoinHook(player);
        }
    }
}
