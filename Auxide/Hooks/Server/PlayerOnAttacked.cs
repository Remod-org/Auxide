using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BasePlayer), "OnAttacked", typeof(HitInfo))]
    public class PlayerOnAttacked
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full) return instr;
            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            Label newLabel = il.DefineLabel();
            int startIndex = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i - 1].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Stloc_0)
                {
                    startIndex = i - 1;
                    //codes[i].labels.Add(newLabel);
                    codes[startIndex].labels.Add(newLabel);
                    break;
                }
            }

            if (startIndex > -1)
            {
                System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Newobj, constr),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnTakeDamageHook")),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Beq_S, newLabel),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(startIndex, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }

        public static void Prefix(BasePlayer __instance, ref HitInfo info)
        {
            if (Auxide.full) return;
            if (info?.WeaponPrefab != null)// && info?.WeaponPrefab is TimedExplosive)
            {
                BaseEntity te = info?.WeaponPrefab;// as TimedExplosive;
                //BasePlayer attacker = info?.InitiatorPlayer;
                BasePlayer attacker = info?.InitiatorPlayer;// as BasePlayer;
                //DamageCheck(__instance, info, attacker, attacked, te);
                DamageCheck(__instance, info, attacker, te);
            }
            else if (info?.Initiator is FireBall || info?.Initiator is FlameTurret || info?.Initiator is FlameThrower)
            {
                // Shortcut for post-bradley fire
                try
                {
                    if (info.HitEntity.ShortPrefabName.Equals("servergibs_bradley") || info.HitEntity.ShortPrefabName.Equals("bradley_crate")) return;
                }
                catch { }
                DamageCheck(__instance, info, null, null);
            }
            else if (info?.InitiatorPlayer != null)
            {
                BasePlayer attacker = info?.InitiatorPlayer;// as BasePlayer;
                DamageCheck(__instance, info, attacker);
            }
        }

        static void DamageCheck(BasePlayer attacked, HitInfo info, BasePlayer attacker, BaseEntity te = null)
        {
            bool isFriend = false;
            bool isFriend2 = false;
            if (attacker != null && attacked != null && info?.Initiator != null)
            {
                isFriend = Utils.IsFriend(attacker.userID, attacked.userID);
                isFriend2 = Utils.IsFriend(info.Initiator.OwnerID, attacked.userID);
            }
            string weapon = te != null ? " using " + te?.GetType() : "";
            //if (Auxide.verbose) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({attacker?.displayName}) to {entity?.ShortPrefabName}({attacked?.displayName}){weapon}");
            //if (Auxide.verbose) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({(info?.Initiator as BasePlayer)?.displayName}) to {entity?.GetType()}({attacked?.displayName}){weapon}");
            if (Auxide.verbose) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({attacker?.displayName}) to BasePlayer({attacked?.displayName}){weapon}");
            if (attacked != null)
            {
                // Attacked is a player, but are they a real player and a friend, etc.
                if (attacker != null && attacked?.userID != attacker?.userID && !isFriend)// && attacked?.userID > 76560000000000000L)
                {
                    if (attacked?.userID < 76560000000000000L)
                    {
                        if (Auxide.config.Options.minimal.allowDamageToNPC)
                        {
                            Utils.DoLog($"Allowing PVP damage by {attacker?.displayName}{weapon} to NPC");
                            return;
                        }
                        Utils.DoLog($"Blocking PVP damage by {attacker?.displayName}{weapon} to NPC");
                        info.damageTypes.ScaleAll(0);
                        return;
                    }

                    // Attacker is a player
                    if (attacker.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                    {
                        if (Auxide.verbose) Utils.DoLog($"Allowing admin damage by {attacker?.displayName}{weapon} to '{attacked?.displayName}'");
                        return;
                    }
                    if (!Auxide.config.Options.minimal.allowPVP && attacker.userID > 76560000000000000L)
                    {
                        Utils.DoLog($"Blocking PVP damage by {attacker?.displayName}{weapon} to '{attacked?.displayName}'");
                        if (te is TimedExplosive) te?.Kill();
                        info.damageTypes.ScaleAll(0);
                    }
                }
                else if (!(info?.Initiator is BasePlayer) && !Auxide.config.Options.minimal.allowPVP && !isFriend && attacker.userID > 76560000000000000L)
                {
                    // Attacker is not a player
                    Utils.DoLog($"Blocking PVP damage by {info?.Initiator?.GetType()}{weapon} to '{attacked?.displayName}'");
                    if (te is TimedExplosive) te?.Kill();
                    info.damageTypes.ScaleAll(0);
                }
            }
        }
    }
}
