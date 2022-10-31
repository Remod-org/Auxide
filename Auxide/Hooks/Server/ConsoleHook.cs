using Harmony;
using UnityEngine;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ConsoleSystem), "Run")]//, new[] { typeof(ConsoleSystem.Option), typeof(string), typeof(object[]) })]
    public class ConsoleHook
    {
        // NOT YET WORKING DO NOT COMPILE
        //public static bool Prefix(ref ConsoleSystem.Option options, string strCommand, ref object[] args)
        public static bool Prefix(ConsoleSystem.Option options, string strCommand, object[] args)
        {
            ConsoleSystem.Arg currentArgs = ConsoleSystem.CurrentArgs;
            if (strCommand != null)
            {
                //if (Auxide.verbose) currentArgs.ReplyWith($"I heard you say '{strCommand}' in console!");
                Auxide.Scripts?.OnChatCommandHook(currentArgs.Player(), strCommand, currentArgs.Args);
            }
            return true;
        }
        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        //{
        //    List<CodeInstruction> codes = new List<CodeInstruction>(instr);
        //    Label newLabel = il.DefineLabel();
        //    int startIndex = -1;

        //    int i;
        //    for (i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i].opcode == OpCodes.Unbox_Any)
        //        {
        //            startIndex = i - 1;
        //            codes[startIndex].labels.Add(newLabel);
        //            break;
        //        }
        //    }

        //    if (startIndex > -1)
        //    {
        //        System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
        //        List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
        //        {
        //            new CodeInstruction(OpCodes.Newobj, constr),
        //            new CodeInstruction(OpCodes.Ldarg_0),
        //            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnConsoleCommandHook")),
        //            new CodeInstruction(OpCodes.Ldnull),
        //            new CodeInstruction(OpCodes.Beq_S, newLabel),
        //            new CodeInstruction(OpCodes.Ret)
        //            //new CodeInstruction(OpCodes.Leave_S, endLabel)
        //        };

        //        codes.InsertRange(startIndex, instructionsToInsert);
        //    }

        //    return codes.AsEnumerable();
        //}
    }
}
