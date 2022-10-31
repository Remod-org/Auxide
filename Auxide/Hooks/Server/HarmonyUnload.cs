using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(HarmonyLoader), "TryUnloadMod")]
    public class HarmonyUnload
    {
        public static void Prefix(ref string name)
        {
            if (name == "Auxide") Auxide.Dispose();
        }
    }
}
