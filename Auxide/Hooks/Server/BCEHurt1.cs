using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Auxide.Hooks.Server
{
    // THIS IS FOR PVP DAMAGE
    [HarmonyPatch(typeof(BaseCombatEntity), "Hurt", typeof(HitInfo))]
    public static class BCEHurt1
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full) return instr;
            List<CodeInstruction> codes = new List<CodeInstruction>(instr);

            Label newLabel = il.DefineLabel();
            int startIndex = -1;

            int i;
            for (i = 0; i < codes.Count; i++)
            {
                //if (codes[i].opcode == OpCodes.Call && codes[i + 4].opcode == OpCodes.Ldfld && startIndex == -1)// && codes[i].operand == typeof(BaseCombatEntity).GetMethod(nameof(DebugHurt))
                if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i+2].opcode == OpCodes.Call && codes[i + 6].opcode == OpCodes.Ldfld && startIndex == -1)// && codes[i].operand == typeof(BaseCombatEntity).GetMethod(nameof(DebugHurt))
                {
                    //startIndex = i - 2;
                    startIndex = i;
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
                    //new CodeInstruction(OpCodes.Ldstr, "OnTakeDamage"),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnTakeDamageHook")),
                    //new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScriptManager), "CallHook")),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Beq_S, newLabel),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(startIndex, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }

        public static void Prefix(BaseCombatEntity __instance, ref HitInfo info)
        {
            if (Auxide.full) return;
            if (info?.WeaponPrefab != null)// && info?.WeaponPrefab is TimedExplosive)
            {
                BaseEntity te = info?.WeaponPrefab;// as TimedExplosive;
                //BasePlayer attacker = info?.InitiatorPlayer;
                BasePlayer attacker = info?.InitiatorPlayer;// as BasePlayer;
                BasePlayer attacked = info?.HitEntity as BasePlayer;
                //DamageCheck(__instance, info, attacker, attacked, te);
                DamageCheck(__instance, info, attacker, attacked, te);
            }
            else if (info?.Initiator is FireBall || info?.Initiator is FlameTurret || info?.Initiator is FlameThrower)
            {
                BasePlayer attacked = info?.HitEntity as BasePlayer;
                // Shortcut for post-bradley fire
                try
                {
                    if (info.HitEntity.ShortPrefabName.Equals("servergibs_bradley") || info.HitEntity.ShortPrefabName.Equals("bradley_crate")) return;
                }
                catch { }
                DamageCheck(__instance, info, null, attacked, null);
            }
            else if (info?.InitiatorPlayer != null)
            {
                //BasePlayer attacker = info?.Initiator as BasePlayer;
                BasePlayer attacker = info?.InitiatorPlayer;// as BasePlayer;
                BasePlayer attacked = info?.HitEntity as BasePlayer;
                DamageCheck(__instance, info, attacker, attacked);
            }
        }

        static void DamageCheck(BaseCombatEntity entity, HitInfo info, BasePlayer attacker, BasePlayer attacked, BaseEntity te = null)
        {
            bool isFriend = false;
            bool isFriend2 = false;
            if (attacker != null && entity != null && info?.Initiator != null)
            {
                isFriend = Utils.IsFriend(attacker.userID, entity.OwnerID);
                isFriend2 = Utils.IsFriend(info.Initiator.OwnerID, entity.OwnerID);
            }
            string weapon = te != null ? " using " + te?.GetType() : "";
            //if (Auxide.verbose) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({attacker?.displayName}) to {entity?.ShortPrefabName}({attacked?.displayName}){weapon}");
            //if (Auxide.verbose) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({(info?.Initiator as BasePlayer)?.displayName}) to {entity?.GetType()}({attacked?.displayName}){weapon}");
            if (Auxide.verbose) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({attacker?.displayName}) to {entity?.GetType()}({attacked?.displayName}){weapon}");
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
            else if (attacker != null && entity?.OwnerID != attacker?.userID && entity?.OwnerID != 0 && !isFriend)
            {
                // Attacker is a player, attacked is null, but victim is entity
                BasePlayer owner = BasePlayer.Find(entity?.OwnerID.ToString());
                if (attacker.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                {
                    if (Auxide.verbose) Utils.DoLog($"Allowing admin damage by {attacker?.displayName}{weapon} to '{entity?.ShortPrefabName}' owned by {owner?.displayName}");
                    return;
                }
                if (!Auxide.config.Options.minimal.allowPVP && attacker.userID > 76560000000000000L)
                {
                    Utils.DoLog($"Blocking PVP damage by {attacker?.displayName}{weapon} to '{entity?.ShortPrefabName}' owned by {owner?.displayName}");
                    if (te is TimedExplosive) te?.Kill();
                    info.damageTypes.ScaleAll(0);
                }
            }
            else if (entity?.OwnerID != info?.Initiator?.OwnerID && entity?.OwnerID != 0 && info?.Initiator?.OwnerID != 0 && !isFriend2)
            {
                // Attacker is an owned entity and victim is an owned entity
                BasePlayer owner = BasePlayer.Find(entity?.OwnerID.ToString());
                BasePlayer attackr = BasePlayer.Find(info?.Initiator?.OwnerID.ToString());
                if (attackr != null && owner != null && attackr.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                {
                    if (Auxide.verbose) Utils.DoLog($"Allowing admin damage by {attackr?.displayName}{weapon} to '{entity?.ShortPrefabName}' owned by {owner?.displayName}");
                    return;
                }
                if (!Auxide.config.Options.minimal.allowPVP && attacker.userID > 76560000000000000L)
                {
                    Utils.DoLog($"Blocking PVP damage from {info?.Initiator?.ShortPrefabName} owned by {attackr?.displayName}{weapon} to '{entity?.ShortPrefabName}'");// owned by {owner?.displayName}");
                    if (te is TimedExplosive) te?.Kill();
                    info.damageTypes.ScaleAll(0);
                }
            }
        }
    }
}
