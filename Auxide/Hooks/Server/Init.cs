using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(Bootstrap), "StartupShared")]
    public class Init
    {
        public static void Prefix() => Auxide.Init();
    }
}
