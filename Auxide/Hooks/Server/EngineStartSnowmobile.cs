using HarmonyLib;
using System.Reflection;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch]
    public static class EngineStartSnowmobile
    {
        static MethodBase TargetMethod()
        {
            System.Type type = (typeof(VehicleEngineController<>));
            type = type.MakeGenericType(new System.Type[] { typeof(Snowmobile) });
            MethodInfo method = type.GetMethod("TryStartEngine", AccessTools.all);
            return method;
        }

        public static void Postfix(Snowmobile __instance)//, ref BasePlayer player)
        {
            Auxide.Scripts.OnEngineStartHook(__instance);//, player);
        }
    }
}
