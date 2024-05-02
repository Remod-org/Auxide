using System;
using HarmonyLib;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ItemContainer), "CanAcceptItem", new Type[] { typeof(Item), typeof(int) })]
    public static class CanAccept
    {
        public static bool Postfix(ItemContainer __instance, ref bool __result, ref Item item, ref int targetPos)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanAcceptItemHook(__instance, item, targetPos);
                if (!(res == null))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
            return true;
        }
    }
}
