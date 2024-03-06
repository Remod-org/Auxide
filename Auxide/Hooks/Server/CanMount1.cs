using Harmony;

namespace Auxide.Hooks.Server
{
    // Compiles but is not being called on mount for chair, minicopter, etc.
    //[HarmonyPatch(typeof(BaseMountable), "MountPlayer", new Type[] { typeof(BasePlayer) })]
    [HarmonyPatch(typeof(BaseMountable), "MountPlayer", typeof(BasePlayer))]
    public static class CanMount1
    {
        //[HarmonyTranspiler]
        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
        //{
        //    if (!Auxide.full) return instr;
        //    List<CodeInstruction> codes = new List<CodeInstruction>(instr);
        //    Label newLabel = il.DefineLabel();
        //    //Label endLabel = il.DefineLabel();
        //    int startIndex = -1;

        //    int i;
        //    for (i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i].opcode == OpCodes.Callvirt && startIndex == -1)
        //        {
        //            startIndex = i - 1;
        //            codes[startIndex].labels.Add(newLabel);
        //            break;
        //        }
        //    }
        //    //codes[i - 1].labels.Add(endLabel);

        //    if (startIndex > -1)
        //    {
        //        System.Reflection.ConstructorInfo constr = typeof(ScriptManager).GetConstructors().First();
        //        List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>()
        //        {
        //            new CodeInstruction(OpCodes.Newobj, constr),
        //            new CodeInstruction(OpCodes.Ldarg_0),
        //            new CodeInstruction(OpCodes.Ldarg_1),
        //            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScriptManager), "CanMountHook")),
        //            //new CodeInstruction(OpCodes.Ldnull),
        //            //new CodeInstruction(OpCodes.Beq_S, newLabel),
        //            //new CodeInstruction(OpCodes.Ret)

        //            new CodeInstruction(OpCodes.Stloc_S, 3),
        //            new CodeInstruction(OpCodes.Ldloc_S, 3),
        //            new CodeInstruction(OpCodes.Isinst, typeof(bool)),
        //            new CodeInstruction(OpCodes.Ldnull),
        //            new CodeInstruction(OpCodes.Beq_S, newLabel),
        //            new CodeInstruction(OpCodes.Ldloc_S, 3),
        //            new CodeInstruction(OpCodes.Unbox_Any, typeof(bool)),
        //            new CodeInstruction(OpCodes.Ret)
        //        };

        //        codes.InsertRange(startIndex, instructionsToInsert);
        //    }

        //    return codes.AsEnumerable();
        //}

        public static void Postfix(BaseMountable __instance, ref BasePlayer player)
        {
            if (!Auxide.full) return;
            Auxide.Scripts?.OnMountedHook(__instance, player);
        }

        public static bool Prefix(BaseMountable __instance, ref BasePlayer player)
        {
            if (Auxide.full)
            {
                object res = Auxide.Scripts?.CanMountHook(__instance, player);
                if (res is bool)
                {
                    return false;
                }
                return true;
            }
            if (__instance?.OwnerID == 0) return true;
            if (player?.userID == __instance?.OwnerID) return true;

            if (__instance.OwnerID == player.userID) return true;
            bool isFriend = Utils.IsFriend(player.userID, __instance.OwnerID);
            if (!isFriend && Auxide.config.Options.minimal.protectMount)
            {
                if (player.IsAdmin && Auxide.config.Options.minimal.allowAdminPVP)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

    }
}
