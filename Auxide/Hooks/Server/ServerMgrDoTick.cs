﻿using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "DoTick")]
    public class ServerMgrDoTick
    {
        [HarmonyPrefix]
        public static void Prefix(ServerMgr __instance)
        {
            if (Auxide.full)
            {
                Auxide.Scripts?.OnTickHook();
            }
        }
    }
}
