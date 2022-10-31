using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseOven), "SVSwitch")]//, typeof(BaseEntity.RPCMessage))]
    public class BaseOvenSV
    {
        public static bool Prefix(BaseOven __instance, ref BaseEntity.RPCMessage msg)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts.CanToggleSwitchHook(__instance, msg.player);
                if (res is bool)
                {
                    return false;
                }
            }
            else
            {
                bool isFriend = Utils.IsFriend(msg.player.userID, __instance.OwnerID);
                if (msg.player?.userID != __instance.OwnerID && !isFriend)
                {
                    if (msg.player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }

        public static void Postfix(BaseOven __instance, ref BaseEntity.RPCMessage msg)
        {
            if (!Auxide.full) return;
            Auxide.Scripts.OnToggleSwitchHook(__instance, msg.player);
        }
    }
}
