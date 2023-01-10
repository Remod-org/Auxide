using Harmony;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(Bootstrap), "StartupShared")]
    public static class Init
    {
        public static void Prefix() => Auxide.Init();
    }
}
