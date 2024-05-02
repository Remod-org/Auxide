using HarmonyLib;
using System;
using UnityEngine;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ServerMgr), "Update")]
    public static class ServerUpdateHook
    {
        [HarmonyPostfix]
        public static void Postfix(ServerMgr __instance)
        {
            if (Auxide.full) // Minimal mode uses only the internal patches and no plugins, compilation, etc.
            {
                Auxide.Scripts?.Update();
                Auxide.Scripts?.Broadcast("Update");
                //try
                //{
                //    Auxide.Scripts?.Update();
                //    Auxide.Scripts?.Broadcast("Update");
                //}
                //catch (Exception e)
                //{
                //    Debug.LogException(e);
                //}
                //return;
            }
        }
    }
}
