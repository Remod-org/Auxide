using HarmonyLib;
using System.Reflection;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch]
    public static class EngineStartPlayerHelicopter
    {
        static MethodBase TargetMethod()
        {
            System.Type type = (typeof(VehicleEngineController<>));
            type = type.MakeGenericType(new System.Type[] { typeof(PlayerHelicopter) });
            MethodInfo method = type.GetMethod("TryStartEngine", AccessTools.all);
            return method;
        }

        public static void Prefix(PlayerHelicopter __instance)//, ref BasePlayer player)
        {
            Utils.DoLog("Calling OnEngineStartHook for PlayerHelicopter");
            FieldInfo field = __instance.GetType().GetField("owner", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PlayerHelicopter ph = field.GetValue(__instance) as PlayerHelicopter;
            Auxide.Scripts.OnEngineStartHook(ph);//, player);
        }
    }
}
