using HarmonyLib;
using System.Reflection;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch]
    public static class EngineStartBaseSubmarine
    {
        static MethodBase TargetMethod()
        {
            System.Type type = (typeof(VehicleEngineController<>));
            type = type.MakeGenericType(new System.Type[] { typeof(BaseSubmarine) });
            MethodInfo method = type.GetMethod("TryStartEngine", AccessTools.all);
            return method;
        }

        public static void Postfix(BaseSubmarine __instance)//, ref BasePlayer player)
        {
            Auxide.Scripts.OnEngineStartHook(__instance);//, player);
        }
    }
}
