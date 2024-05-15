using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(DecayEntity), "DecayTick")]
    public static class DecayTickHooks
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full) return instr;
            List<CodeInstruction> codes = new List<CodeInstruction>(instr);

            Label newLabelA = il.DefineLabel();
            Label newLabelB = il.DefineLabel();
            int startIndexA = -1;
            int startIndexB = -1;

            int i;
            for (i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Calli && codes[i+1].opcode == OpCodes.Ldc_R4 && codes[i + 2].opcode == OpCodes.Ble_Un_S && startIndexA == -1)
                {
                    startIndexA = i + 2;
                    codes[startIndexA].labels.Add(newLabelA);
                    break;
                }
                if (codes[i].opcode == OpCodes.Ldloc_S && codes[i+1].opcode == OpCodes.Ldc_R4 && codes[i + 2].opcode == OpCodes.Ble_Un_S && startIndexA > -1 && startIndexB == -1)
                {
                    startIndexB = i + 2;
                    codes[startIndexB].labels.Add(newLabelB);
                    break;
                }
            }

            if (startIndexA > -1)
            {
                System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Newobj, constr),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnDecayHealHook")),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Beq_S, newLabelA),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(startIndexA, instructionsToInsert);
            }

            if (startIndexB > -1)
            {
                System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Newobj, constr),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnDecayDamageHook")),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Beq_S, newLabelB),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(startIndexB, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }
    }
}
