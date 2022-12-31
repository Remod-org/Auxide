using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseCombatEntity), "Die", typeof(HitInfo))]
    public class BCEDie1
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full) return instr;

            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            int startIndex = -1;
            int fixJump = -1;
            Label newLabel = il.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Brfalse_S && codes[i + 1].opcode == OpCodes.Ldarg_1)
                {
                    startIndex = i + 1;
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
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnEntityDeathHook"))
                };

                codes.InsertRange(startIndex, instructionsToInsert);
                codes[startIndex].labels.Add(newLabel);
            }

            if (fixJump > -1)
            {
                // Fix jump from saveInfo.msg.baseNetworkable == null to avoid skipping our new code
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Brtrue_S, newLabel)
                };
                codes.RemoveRange(fixJump, 1);
                codes.InsertRange(fixJump, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }
    }
}
