using HarmonyLib;
using System.Reflection;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch]
    public static class EngineStartGroundVehicle
    {
        static MethodBase TargetMethod()
        {
            System.Type type = (typeof(VehicleEngineController<>));
            type = type.MakeGenericType(new System.Type[] { typeof(GroundVehicle) });
            MethodInfo method = type.GetMethod("TryStartEngine", AccessTools.all);
            return method;
        }

        public static void Postfix(GroundVehicle __instance)//, ref BasePlayer player)
        {
            Auxide.Scripts.OnEngineStartHook(__instance);//, player);
        }
    }
}
