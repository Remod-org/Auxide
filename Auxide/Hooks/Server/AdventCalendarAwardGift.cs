using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(AdventCalendar), "AwardGift")]
    public static class AdventCalendarAwardGift
    {
        public static bool Prefix(AdventCalendar __instance, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.OnAdventGiftAwardHook(__instance, player);
                if (res is bool)
                {
                    return false;
                }
            }
            return true;
        }

        public static void Postfix(AdventCalendar __instance, ref BasePlayer player)
        {
            if (!Auxide.full) return;
            Auxide.Scripts.OnAdventGiftAwardedHook(__instance, player);
        }
    }

    [HarmonyPatch(typeof(AdventCalendar), "WasAwardedTodaysGift")]
    public static class WasAwardedTodaysGift
    {
        public static bool Prefix(AdventCalendar __instance, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanBeAwardedAdventGiftHook(__instance, player);
                if (res is bool)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
