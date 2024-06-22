using HarmonyLib;
using System.Reflection;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch]
    public static class EngineStartMinicopter
    {
        static MethodBase TargetMethod()
        {
            System.Type type = (typeof(VehicleEngineController<>));
            type = type.MakeGenericType(new System.Type[] { typeof(Minicopter) });
            MethodInfo method = type.GetMethod("TryStartEngine", AccessTools.all);
            return method;
        }

        public static void Prefix(Minicopter __instance)//, ref BasePlayer player)
        {
            Utils.DoLog("Calling OnEngineStartHook for Minicopter");
            Auxide.Scripts.OnEngineStartHook(__instance);//, player);
        }
    }
}
