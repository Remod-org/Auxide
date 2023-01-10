using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(ElectricSwitch), "SVSwitch")]//, typeof(BaseEntity.RPCMessage))]
    public static class ElecSwitchSV
    {
        public static bool Prefix(ElectricSwitch __instance, ref BaseEntity.RPCMessage msg)
        {
            object res = Auxide.Scripts.CanToggleSwitchHook(__instance, msg.player);
            if (res is bool)
            {
                return false;
            }
            return true;
        }

        public static void Postfix(BaseOven __instance, ref BaseEntity.RPCMessage msg)
        {
            Auxide.Scripts.OnToggleSwitchHook(__instance, msg.player);
        }

        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        //{
        //    List<CodeInstruction> codes = new List<CodeInstruction>(instr);
        //    Label newLabel = il.DefineLabel();
        //    int startIndex = 0;

        //    System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
        //    List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
        //    {
        //        new CodeInstruction(OpCodes.Newobj, constr),
        //        new CodeInstruction(OpCodes.Ldarg_0),
        //        new CodeInstruction(OpCodes.Ldarg_1),
        //        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptManager), "CanToggleSwitchHook")),
        //        new CodeInstruction(OpCodes.Ldnull),
        //        new CodeInstruction(OpCodes.Beq_S, newLabel),
        //        new CodeInstruction(OpCodes.Ret)
        //    };

        //    codes.InsertRange(startIndex, instructionsToInsert);
        //    codes[startIndex].labels.Add(newLabel);

        //    return codes.AsEnumerable();
        //}
    }
}
