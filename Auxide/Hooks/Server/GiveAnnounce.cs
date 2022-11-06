﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ConVar;
using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(Inventory), "give", typeof(ConsoleSystem.Arg))]
    public class GiveAnnounce
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instr);

            Label newLabel = il.DefineLabel();
            int startIndex = -1;

            int i;
            for (i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_5 && codes[i + 1].opcode == OpCodes.Newarr && startIndex == -1)
                {
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
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Auxide), "hideGiveNotices")),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality")),
                    new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                    new CodeInstruction(OpCodes.Ret)
                };
                codes.InsertRange(startIndex, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }
    }
}