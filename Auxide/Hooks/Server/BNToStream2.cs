using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(BaseNetworkable), "ToStream", new Type[] { typeof(Stream), typeof(BaseNetworkable.SaveInfo) })]
    public static class BNToStreamPatch2
    {
        // This patch calls OnEntitySavedHook for full mode
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            if (!Auxide.full)
            {
                //return instr;
                return null;
            }

            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            int startIndex = -1;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_2 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldarg_1 && startIndex == -1)
                {
                    startIndex = i;
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
                    new CodeInstruction(OpCodes.Ldarg_2),
                    //new CodeInstruction(OpCodes.Box, typeof(BaseNetworkable.SaveInfo)),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnEntitySavedHook")),
                    new CodeInstruction(OpCodes.Pop)
                };

                codes.InsertRange(startIndex, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }
        //public static void Postfix(BaseNetworkable __instance, Stream stream, BaseNetworkable.SaveInfo saveInfo)
        //{
        //    if (!Auxide.full)
        //    {
        //        return;
        //    }
        //    //Auxide.Scripts.OnEntitySavedHook(__instance, saveInfo);
        //    saveInfo.msg.ToProto(stream);
        //    __instance.PostSave(saveInfo);
        //}
    }
}
