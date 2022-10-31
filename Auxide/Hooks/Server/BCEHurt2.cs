using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using Rust;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseCombatEntity), "Hurt", new Type[] { typeof(float), typeof(DamageType), typeof(BaseEntity), typeof(bool) })]
    public class BCEHurt2
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full) return instr;
            List<CodeInstruction> codes = new List<CodeInstruction>(instr);

            Label newLabel = il.DefineLabel();
            int startIndex = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Calli)// && codes[i].operand == typeof(BaseCombatEntity).GetMethod(nameof(DebugHurt))
                {
                    startIndex = i - 2;
                    codes[i].labels.Add(newLabel);
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
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScriptManager), "OnTakeDamageHook")),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Beq_S, newLabel),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(startIndex, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }

        public static void Prefix(BaseCombatEntity __instance, ref float amount, ref DamageType type, ref BaseEntity attacker, ref bool useProtection)
        {
            if (Auxide.full) return;
            if (Auxide.config.Options.minimal.blockBuildingDecay || Auxide.config.Options.minimal.blockDeployablesDecay)
            {
                //if (verbose) Debug.LogWarning($"Decay called for {__instance.GetType().Name}({__instance.ShortPrefabName})");
                if (type == DamageType.Decay)
                {
                    if ((__instance as BuildingBlock) != null && __instance.OwnerID != 0 && Auxide.config.Options.minimal.blockBuildingDecay)
                    {
                        if (Auxide.verbose) Utils.DoLog($"Blocking building block decay on {__instance.ShortPrefabName}");
                        amount = 0;
                    }
                    else if (__instance.OwnerID != 0 && Auxide.config.Options.minimal.blockDeployablesDecay)
                    {
                        if (Auxide.verbose) Utils.DoLog($"Blocking deployable decay on {__instance.ShortPrefabName}");
                        amount = 0;
                    }
                }
            }
        }
    }
}
