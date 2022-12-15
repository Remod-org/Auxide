using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection.Emit;
using Harmony;
using ProtoBuf;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseNetworkable), "ToStream", new Type[] { typeof(Stream), typeof(BaseNetworkable.SaveInfo) })]
    public class BNToStreamPatch1
    {
        // This patch disables the TC decay warning in minimal mode
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (Auxide.full || !Auxide.config.Options.minimal.disableTCWarning)
            {
                return instr;
            }

            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            int startIndex = -1;
            int fixJump = -1;
            Label notBPLabel = new Label();
            Label newLabel = il.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i + 1].opcode == OpCodes.Brtrue)
                {
                    fixJump = i + 1;
                }

                if (codes[i].opcode == OpCodes.Ldarg_2 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldarg_1)
                {
                    startIndex = i;
                    notBPLabel = codes[i].labels[0];
                    break;
                }
            }

            if (startIndex > -1)
            {
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
                {
                    // is type of BuildingPrivlidge?
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Isinst, typeof(BuildingPrivlidge)),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality")),
                    new CodeInstruction(OpCodes.Brfalse_S, notBPLabel),

                    // saveInfo.msg.buildingPrivilege.protectedMinutes = 4400;
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BaseNetworkable.SaveInfo), "msg")),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Entity), "buildingPrivilege")),
                    new CodeInstruction(OpCodes.Ldc_R4, 4400.1f),
                    new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(BuildingPrivilege), "protectedMinutes")),
                    // saveInfo.msg.buildingPrivilege.upkeepPeriodMinutes = 4400;
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BaseNetworkable.SaveInfo), "msg")),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Entity), "buildingPrivilege")),
                    new CodeInstruction(OpCodes.Ldc_R4, 4400.1f),
                    new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(BuildingPrivilege), "upkeepPeriodMinutes"))
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

    [HarmonyPatch(typeof(BaseNetworkable), "ToStream", new Type[] { typeof(Stream), typeof(BaseNetworkable.SaveInfo) })]
    public class BNToStreamPatch2
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full)
            {
                return instr;
            }

            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            Label newLabel = il.DefineLabel();
            int fixJump = -1;
            int startIndex = -1;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i + 1].opcode == OpCodes.Brtrue)
                {
                    fixJump = i + 1;
                }

                if (codes[i].opcode == OpCodes.Ldarg_2 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldarg_1 && startIndex == -1)
                {
                    startIndex = i;// + 3;
                    break;
                }
            }

            if (startIndex > -1)
            {
                System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Newobj, constr),
                    new CodeInstruction(OpCodes.Ldstr, "OnEntitySavedHook"),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Box, typeof(BaseNetworkable.SaveInfo)),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScriptManager), "CallHook")),
                    new CodeInstruction(OpCodes.Pop)
                };

                codes.InsertRange(startIndex, instructionsToInsert);
                codes[startIndex].labels.Add(newLabel);
                //codes.RemoveAt(startIndex + 6);
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
