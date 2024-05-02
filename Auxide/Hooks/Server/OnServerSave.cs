using HarmonyLib;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(SaveRestore), "DoAutomatedSave")]
    public static class OnServerSave
    {
        internal static TimeSince _lastcall;
        public static void Prefix()
        {
            if (Auxide.full)
            {
                if (_lastcall <= 0.5f)
                {
                    return;
                }
                Auxide.Scripts?.OnServerSaveHook();
                _lastcall = 0f;
            }
            Auxide.LoadConfig();
        }
    }
}
