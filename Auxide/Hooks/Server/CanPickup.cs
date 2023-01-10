using Harmony;

namespace Auxide.Hooks.Server
{
    // UNTESTED 11-03-2022
    [HarmonyPatch(typeof(ContainerIOEntity), "CanPickup", typeof(BasePlayer))]
    public static class CanPickup1
    {
        public static bool Prefix(ContainerIOEntity __instance, ref bool __result, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanPickupHook(__instance, player);
                if (res is bool)
                {
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
                if (!(player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP))
                {
                    BasePlayer owner = BasePlayer.FindByID(__instance.OwnerID);
                    if (Auxide.verbose) Utils.DoLog($"Blocking pickup for {player?.displayName} to {__instance.ShortPrefabName} owned by {owner?.displayName}");
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StorageContainer), "CanPickup", typeof(BasePlayer))]
    public static class CanPickup2
    {
        public static bool Prefix(StorageContainer __instance, ref bool __result, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanPickupHook(__instance, player);
                if (res is bool)
                {
                    __result = false;
                    return false;
                }
                return true;
            }

            if (__instance.OwnerID == player.userID) return true;

            bool isFriend = Utils.IsFriend(player.userID, __instance.OwnerID);
            if (player.userID != __instance.OwnerID && __instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.protectLoot)
            {
                if (!(player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP))
                {
                    BasePlayer owner = BasePlayer.FindByID(__instance.OwnerID);
                    if (Auxide.verbose) Utils.DoLog($"Blocking pickup for {player?.displayName} to {__instance.ShortPrefabName} owned by {owner?.displayName}");
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(BaseCombatEntity), "CanPickup", typeof(BasePlayer))]
    public static class CanPickup3
    {
        public static bool Prefix(BaseCombatEntity __instance, ref bool __result, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanPickupHook(__instance, player);
                if (res is bool)
                {
                    __result = false;
                    return false;
                }
                return true;
            }

            if (__instance.OwnerID == player.userID) return true;

            bool isFriend = Utils.IsFriend(player.userID, __instance.OwnerID);
            if (player.userID != __instance.OwnerID && __instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.protectLoot)
            {
                if (!(player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP))
                {
                    BasePlayer owner = BasePlayer.FindByID(__instance.OwnerID);
                    if (Auxide.verbose) Utils.DoLog($"Blocking pickup for {player?.displayName} to {__instance.ShortPrefabName} owned by {owner?.displayName}");
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
