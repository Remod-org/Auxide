using HarmonyLib;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(TinCanAlarm), "TriggerAlarm")]
    public static class TriggerAlarmPatch
    {
        [HarmonyPrefix]
        static void Prefix(TinCanAlarm __instance)
        {
            Auxide.Scripts.OnTinCanAlarmTriggerHook(__instance);
        }
    }
}
