using HarmonyLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseNetworkable), "ToStream", new Type[] { typeof(Stream), typeof(BaseNetworkable.SaveInfo) })]
    public static class BNToStreamPatch1
    {
        // This patch disables the TC decay warning in minimal mode
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (Auxide.full)
            {
                return null;
            }
            if (!Auxide.config.Options.minimal.disableTCWarning)
            {
                //return instr;
                return null;
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

        //public static void Postfix(BaseNetworkable __instance, Stream stream, ref BaseNetworkable.SaveInfo saveInfo)
        //{
        //    if (!Auxide.full)
        //    {
        //        if (__instance.gameObject.GetComponent<BuildingPrivilege>() != null)
        //        {
        //            saveInfo.msg.buildingPrivilege.protectedMinutes = 4400f;
        //            saveInfo.msg.buildingPrivilege.upkeepPeriodMinutes = 4400f;
        //        }
        //        return;
        //    }
        //    if (saveInfo.forConnection == null) return;
        //    Auxide.Scripts.OnEntitySavedHook(__instance, saveInfo);
        //    saveInfo.msg?.ToProto(stream);
        //    __instance?.PostSave(saveInfo);
        //}
    }
}
