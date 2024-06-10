using HarmonyLib;
using UnityEngine;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ConsoleSystem), "Internal")]
    public static class ConsoleHook
    {
        public static bool Prefix(ConsoleSystem.Arg arg, ref bool __result)
        {
            if (Auxide.full)
            {
                if (arg.Invalid)
                {
                    __result = false;
                    return false;
                }
                string cmd = arg.cmd.FullName;
                BasePlayer player = arg.Connection?.player as BasePlayer;
                object obj = Auxide.Scripts?.OnConsoleCommandHook(player, cmd, Utils.ExtractArgs(arg));
                if (obj is bool)
                {
                    __result = false;
                    return false;// (bool)obj;
                }
            }
            return true;
        }
    }
    //[HarmonyPatch(typeof(ConsoleSystem), "Run", new Type[] { typeof(ConsoleSystem.Option), typeof(string), typeof(object[]) })]
    //public static class ConsoleHook
    //{
    //    // NOT YET WORKING DO NOT COMPILE
    //    //public static bool Prefix(ref ConsoleSystem.Option options, ref string __result, ref string strCommand, ref object[] args)
    //    //{
    //    //    __result = (string)Auxide.Scripts.OnConsoleCommandHook(strCommand, args);
    //    //    return true;
    //    //}
    //    private static readonly Dictionary<int, string> ignoredStart = new Dictionary<int, string>()
    //    {
    //        { 2, "ai" },
    //        { 3, "app" },
    //        { 3, "fps" },
    //        { 4, "mlrs" },
    //        { 4, "rcon" },
    //        { 6, "craft." },
    //        { 6, "global" },
    //        { 7, "boombox" },
    //        { 7, "logfile" },
    //        { 7, "ownerid" },
    //        { 7, "server." },
    //        { 8, "chat.say" },
    //        { 11, "nav_disable" }
    //    };
    //    //public static bool Prefix(ConsoleSystem __instance, ref bool __result, ConsoleSystem.Option options, string strCommand, object[] args)
    //    public static bool Prefix(ConsoleSystem.Option options, string strCommand, object[] args)
    //    {
    //        //ConsoleSystem.Arg currentArgs = ConsoleSystem.CurrentArgs;
    //        //string str = BuildCommand(strCommand, args);
    //        //__instance.Option = options;
    //        //Arg arg = new Arg(options, str);
    //        //bool flag = arg.HasPermission();
    //        //if (strCommand != null)
    //        //{
    //        //    //if (Auxide.verbose) currentArgs.ReplyWith($"I heard you say '{strCommand}' in console!");
    //        //    Auxide.Scripts?.OnChatCommandHook(currentArgs.Player(), strCommand, currentArgs.Args);
    //        //}

    //        //if (ignoredStart.ContainsValue(strCommand.Trim()))
    //        //{
    //        //    return true;
    //        //}
    //        //foreach (KeyValuePair<int, string> ign in ignoredStart)
    //        //{
    //        //    if (strCommand.Length >= ign.Key - 1 && strCommand.Substring(0, ign.Key) == ign.Value) return true;
    //        //}
    //        object res = Auxide.Scripts?.OnConsoleCommandHook(strCommand, args);
    //        //if (res != null)
    //        //{
    //        //    __result = false;
    //        //    return false;
    //        //}
    //        return true;
    //    }

    //    //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
    //    //{
    //    //    List<CodeInstruction> codes = new List<CodeInstruction>(instr);
    //    //    Label newLabel = il.DefineLabel();
    //    //    int startIndex = -1;

    //    //    int i;
    //    //    for (i = 0; i < codes.Count; i++)
    //    //    {
    //    //        if (codes[i].opcode == OpCodes.Ldloc_2 && codes[i - 1].opcode == OpCodes.Stsfld && codes[i - 2].opcode == OpCodes.Ldstr)
    //    //        {
    //    //            startIndex = i - 2;
    //    //            codes[startIndex].labels.Add(newLabel);
    //    //            break;
    //    //        }
    //    //    }

    //    //    if (startIndex > -1)
    //    //    {
    //    //        System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
    //    //        List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
    //    //        {
    //    //            new CodeInstruction(OpCodes.Newobj, constr),
    //    //            new CodeInstruction(OpCodes.Ldarg_0),
    //    //            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScriptManager), "OnConsoleCommandHook")),
    //    //            //new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "OnConsoleCommandHook")),
    //    //            new CodeInstruction(OpCodes.Stloc_S, 5),
    //    //            new CodeInstruction(OpCodes.Ldloc_S, 5),
    //    //            new CodeInstruction(OpCodes.Isinst, typeof(bool)),
    //    //            new CodeInstruction(OpCodes.Ldnull),
    //    //            new CodeInstruction(OpCodes.Beq_S, newLabel),
    //    //            new CodeInstruction(OpCodes.Ldloc_S, 5),
    //    //            new CodeInstruction(OpCodes.Unbox_Any, typeof(bool)),
    //    //            new CodeInstruction(OpCodes.Ret)
    //    //            //new CodeInstruction(OpCodes.Leave_S, endLabel)
    //    //        };

    //    //        codes.InsertRange(startIndex, instructionsToInsert);
    //    //    }

    //    //    return codes.AsEnumerable();
    //    //}
    //}
}
