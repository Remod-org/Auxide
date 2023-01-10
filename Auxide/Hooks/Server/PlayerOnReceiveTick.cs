using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BasePlayer), "OnReceiveTick", new Type[] { typeof(PlayerTick), typeof(bool) })]
    public static class PlayerOnReceiveTick
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full) return instr;

            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            CodeInstruction serverInput = new CodeInstruction(OpCodes.Ldarg_0);
            Label newLabel1 = il.DefineLabel();
            Label newLabel2 = il.DefineLabel();
            int startIndexOPT = -1;
            int startIndexOPI = -1;

            for (int i = 0; i < codes.Count; i++)
            {
                // Insert before inputstate current/previous comparison
                if (codes[i].opcode == OpCodes.Ldarg_0 &&
                    codes[i + 1].opcode == OpCodes.Call &&
                    codes[i + 2].opcode == OpCodes.Ldfld &&
                    codes[i + 3].opcode == OpCodes.Ldfld &&
                    startIndexOPT == -1)
                {
                    startIndexOPT = i;
                    codes[startIndexOPT].labels.Add(newLabel1);
                }
                // Insert before call to IsReceivingSnapshot
                else if (codes[i].opcode == OpCodes.Beq &&
                    codes[i + 1].opcode == OpCodes.Ldarg_0 &&
                    //codes[i + 2].opcode == OpCodes.Call &&
                    startIndexOPI == -1)
                {
                    startIndexOPI = i + 3;
                    codes[startIndexOPI].labels.Add(newLabel2);
                }
                else if (codes[i].opcode == OpCodes.Ldfld &&
                    codes[i + 1].opcode == OpCodes.Ldfld &&
                    codes[i + 2].opcode == OpCodes.Ldarg_0
                    )
                {
                    serverInput = new CodeInstruction(codes[i + 3]);
                }
            }

            //if (startIndexOPT > -1)
            //{
            //    System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
            //    List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
            //    {
            //        new CodeInstruction(OpCodes.Newobj, constr),
            //        new CodeInstruction(OpCodes.Ldarg_0),
            //        new CodeInstruction(OpCodes.Ldarg_1),
            //        new CodeInstruction(OpCodes.Ldarg_2),
            //        new CodeInstruction(OpCodes.Box, typeof(bool)),
            //        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnPlayerTickHook")),
            //        new CodeInstruction(OpCodes.Ldnull),
            //        new CodeInstruction(OpCodes.Beq_S, newLabel1),
            //        new CodeInstruction(OpCodes.Ret)
            //    };

            //    codes.InsertRange(startIndexOPT, instructionsToInsert);
            //}

            if (startIndexOPI > -1)
            {
                System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Newobj, constr),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    //new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InputState), "get_serverInput")),
                    new CodeInstruction(serverInput),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnPlayerInputHook")),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Beq_S, newLabel2),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(startIndexOPI, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }
    }
}
