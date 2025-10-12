using HarmonyLib;
using Logging;
using Serilog;
using UI.Menu;

namespace Railroader.DummyMod.Harmony
{
    [HarmonyPatch]
    public static class MainMenuPatch
    {
        public static int Number = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenu), "ShouldShowEditor")]
        public static bool ShouldShowEditorPrefix(MainMenu __instance) {
            Log.Information("--- MainMenu::ShouldShowEditor::Prefix patch from dummy called: " + DummyPlugin.Instance?.IsEnabled);
            return true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "ShouldShowEditor")]
        public static void ShouldShowEditorPostfix(MainMenu __instance) {
            Log.Information("--- MainMenu::ShouldShowEditor::Postfix patch from dummy called: " + DummyPlugin.Instance?.IsEnabled);
        }
    }
}
