using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BuildingPrivlidge), "CanAdministrate", typeof(BasePlayer))]
    public class CanAdminTC
    {
        public static bool Prefix(BuildingPrivlidge __instance, ref bool __result, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanAdminTCHook(__instance, player);
                if (res is bool)
                {
                    __result = false;
                    return false;
                }
                return true;
            }

            bool isFriend = Utils.IsFriend(player.userID, __instance.OwnerID);
            if (player.userID != __instance.OwnerID && __instance.OwnerID != 0 && !isFriend && Auxide.config.Options.minimal.blockTCMenu)
            {
                BasePlayer owner = BasePlayer.FindByID(__instance.OwnerID);
                if (player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                {
                    if (Auxide.verbose) Utils.DoLog($"Allowing administration access by ADMIN {player?.displayName} to TC owned by {owner?.displayName}");
                }
                else
                {
                    if (Auxide.verbose) Utils.DoLog($"Blocking administration access for {player?.displayName} to TC owned by {owner?.displayName}");
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
