using System;
using Harmony;
using UnityEngine;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "Update")]
    public class ServerUpdateHook
    {
        [HarmonyPostfix]
        public static void Postfix(ServerMgr __instance)
        {
            if (Auxide.full) // Minimal mode uses only the internal patches and no plugins, compilation, etc.
            {
                try
                {
                    Auxide.Scripts?.Update();
                    Auxide.Scripts?.Broadcast("Update");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                return;
            }
        }
    }
}
