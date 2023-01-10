using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseCombatEntity), "OnPickedUp")]
    public static class BCEOnPickedUp
    {
        public static void Prefix(BaseCombatEntity __instance, ref Item createdItem, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                Auxide.Scripts.OnPickedUpHook(__instance, createdItem, player);
            }
        }
    }
}
