using Harmony;
using System.Reflection;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(MarketTerminal), "CanPlayerInteract")]
    public static class MarketTerminalPatch
    {
        static bool Prefix(ref MarketTerminal __instance, ref bool __result, BasePlayer player)
        {
            if (__instance == null) return true;
            if (player == null) return false;
            object obj = Auxide.Scripts.CanPlayerAccessMarketHook(__instance, player);
            if (obj == null)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(MarketTerminal), "UpdateHasItems")]
    public static class MarketTerminalPatch2
    {
        static void Postfix(ref MarketTerminal __instance)
        {
            Auxide.Scripts.OnMarketUpdateHook(__instance);
            FieldInfo customerIdField = typeof(MarketTerminal).GetField("_customerSteamId", BindingFlags.NonPublic | BindingFlags.Instance) ?? typeof(MarketTerminal).GetField("_customerSteamId", BindingFlags.Public | BindingFlags.Instance);
            ulong customerid = (ulong)customerIdField.GetValue(__instance);
            if (__instance?.inventory.itemList.Count > 0 && customerid != 0)
            {
                if (__instance.pendingOrders.Count > 0)
                {
                    Auxide.Scripts.OnMarketOrderStartHook(__instance, customerid);
                }
                else
                {
                    Auxide.Scripts.OnMarketOrderFinishHook(__instance, customerid);
                }
            }
        }
    }
}
