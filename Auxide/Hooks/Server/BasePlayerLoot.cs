using System;
using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BasePlayer), "CanBeLooted", new Type[] { typeof(BasePlayer) })]
    public class BasePlayerLoot
    {
        // Need to test
        public static bool Prefix(StorageContainer __instance, ref bool __result, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanLootHook(__instance, player, "");
                if (res is bool)
                {
                    // FIXME : RPC ERROR :(

                    __result = false;
                    return false;
                }
                return true;
            }

            if (!Auxide.config.Options.minimal.protectSleeper) return true;
            if (__instance.OwnerID == player.userID) return true;

            bool isFriend = Utils.IsFriend(player.userID, __instance.OwnerID);
            BasePlayer owner = BasePlayer.FindByID(__instance.OwnerID);
            if (Auxide.verbose) Utils.DoLog($"{player?.displayName} trying to loot {owner?.displayName}");
            if (__instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.protectLoot)
            {
                if (player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                {
                    if (Auxide.verbose) Utils.DoLog($"Allowing admin access for {player?.displayName} to {__instance?.ShortPrefabName} owned by {owner?.displayName}");
                    return true;
                }

                if (Auxide.verbose) Utils.DoLog($"Blocking access for {player?.displayName} to {__instance?.ShortPrefabName} owned by {owner?.displayName}");
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
