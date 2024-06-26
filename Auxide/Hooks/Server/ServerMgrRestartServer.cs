﻿using HarmonyLib;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "RestartServer")]
    public static class ServerMgrRestartServer
    {
        [HarmonyPrefix]
        public static void Prefix(ServerMgr __instance)
        {
            if (Auxide.full)
            {
                Auxide.Scripts?.UnloadAll();
            }
        }
    }
}
