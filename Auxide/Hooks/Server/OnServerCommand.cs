using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ConsoleSystem), "Internal", typeof(ConsoleSystem.Arg))]
    public class OnServerCommand
    {
        // NOT YET WORKING DO NOT COMPILE
        //public static bool Prefix(ref ConsoleSystem.Option options, ref string __result, ref string strCommand, ref object[] args)
        //{
        //    __result = (string)Auxide.Scripts.OnConsoleCommandHook(strCommand, args);
        //    return true;
        //}
        //public static bool Prefix(ConsoleSystem __instance, ConsoleSystem.Option options, string strCommand, object[] args)
        //{
        //    //ConsoleSystem.Arg currentArgs = ConsoleSystem.CurrentArgs;
        //    __instance.BuildC
        //    if (strCommand != null)
        //    {
        //        //if (Auxide.verbose) currentArgs.ReplyWith($"I heard you say '{strCommand}' in console!");
        //        Auxide.Scripts?.OnChatCommandHook(currentArgs.Player(), strCommand, currentArgs.Args);
        //    }
        //    return true;
        //}
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instr);
            Label newLabel = il.DefineLabel();
            int startIndex = -1;

            int i;
            for (i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i -1].opcode == OpCodes.Ldarg_0)
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
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScriptManager), "OnServerCommandHook")),
                    //new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnConsoleCommandHook")),
                    new CodeInstruction(OpCodes.Stloc_S, 5),
                    new CodeInstruction(OpCodes.Ldloc_S, 5),
                    new CodeInstruction(OpCodes.Isinst, typeof(bool)),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Beq_S, newLabel),
                    new CodeInstruction(OpCodes.Ldloc_S, 5),
                    new CodeInstruction(OpCodes.Unbox_Any, typeof(bool)),
                    new CodeInstruction(OpCodes.Ret)
                    //new CodeInstruction(OpCodes.Leave_S, endLabel)
                };

                codes.InsertRange(startIndex, instructionsToInsert);
            }

            return codes.AsEnumerable();
        }
    }
}
