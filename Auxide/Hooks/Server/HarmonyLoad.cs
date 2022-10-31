using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(HarmonyLoader), "TryLoadMod")]
    public class HarmonyLoad
    {
        public static void Postfix(ref string dllName)
        {
            if (dllName == "Auxide") Auxide.Init();
        }
    }
}
