using Harmony;
using System;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(PlayerCorpse), "OnStartBeingLooted", new Type[] { typeof(BasePlayer) })]
    public static class CorpseLoot
    {
        // NOT YET WORKING 15-Nov-2022
        public static bool Prefix(PlayerCorpse __instance, ref bool __result, ref BasePlayer baseEntity)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanLootHook(__instance, baseEntity, "");
                if (res is bool)
                {
                    // FIXME : RPC ERROR :(

                    __result = false;
                    return false;
                }
                return true;
            }

            if (!Auxide.config.Options.minimal.protectCorpse) return true;
            if (__instance.OwnerID == baseEntity.userID)
            {
                __result = true;
                return true;
            }

            bool isFriend = Utils.IsFriend(baseEntity.userID, __instance.OwnerID);
            BasePlayer owner = BasePlayer.FindByID(__instance.OwnerID);
            if (Auxide.verbose) Utils.DoLog($"{baseEntity?.displayName} trying to loot corpse of {owner?.displayName}");
            //if (player.userID != __instance.OwnerID && __instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.protectLoot)
            if (__instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.protectCorpse)
            {
                if (!(baseEntity.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP))
                {
                    if (Auxide.verbose) Utils.DoLog($"Blocking access for {baseEntity?.displayName} to {__instance?.ShortPrefabName} of {owner?.displayName}");
                    __result = false;
                    return false;
                }
            }
            return true;
        }

        public static void Postfix(PlayerCorpse __instance, ref BasePlayer baseEntity)
        {
            if (Auxide.full)
            {
                Auxide.Scripts.OnLootedHook(__instance, baseEntity);
            }
        }
    }
}
