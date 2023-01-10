using System;
using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(StorageContainer), "CanOpenLootPanel", new Type[] { typeof(BasePlayer), typeof(string) })]
    public static class SCLoot
    {
        // Working 11-03-2022 (minimal)
        public static bool Prefix(StorageContainer __instance, ref bool __result, ref BasePlayer player, ref string panelName)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanLootHook(__instance, player, panelName);
                if (res is bool)
                {
                    // FIXME : RPC ERROR :(

                    __result = false;
                    return false;
                }
                return true;
            }

            if (__instance.OwnerID == player.userID) return true;

            bool isFriend = Utils.IsFriend(player.userID, __instance.OwnerID);
            //if (player.userID != __instance.OwnerID && __instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.protectLoot)
            if (__instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.protectLoot)
            {
                BasePlayer owner = BasePlayer.FindByID(__instance.OwnerID);
                if (player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                {
                    if (Auxide.verbose) Utils.DoLog($"Allowing admin access for {player?.displayName} to {__instance?.ShortPrefabName}({panelName}) owned by {owner?.displayName}");
                    return true;
                }

                if (Auxide.verbose) Utils.DoLog($"Blocking access for {player?.displayName} to {__instance?.ShortPrefabName}({panelName}) owned by {owner?.displayName}");
                __result = false;
                return false;
            }
            return true;
        }

        public static void Postfix(StorageContainer __instance, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                Auxide.Scripts.OnLootedHook(__instance, player);
            }
        }
    }
}
