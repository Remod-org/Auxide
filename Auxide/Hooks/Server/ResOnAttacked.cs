using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ResourceEntity), "OnAttacked", typeof(HitInfo))]
    public static class ResOnAttacked
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full) return instr;
            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            Label newLabel = il.DefineLabel();
            int startIndex = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldnull)
                {
                    startIndex = i - 1;
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
    }
}
