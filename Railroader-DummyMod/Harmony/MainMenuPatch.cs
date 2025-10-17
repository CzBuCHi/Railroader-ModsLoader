using HarmonyLib;
using JetBrains.Annotations;
using UI.Menu;

namespace Railroader.DummyMod.Harmony
{
    [HarmonyPatch]
    [UsedImplicitly]
    public static class MainMenuPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenu), "ShouldShowEditor")]
        public static bool ShouldShowEditorPrefix() {
            DummyPlugin.Instance.Logger.Information("--- MainMenu::ShouldShowEditor::Prefix patch from dummy called: " + DummyPlugin.Instance?.IsEnabled);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "ShouldShowEditor")]
        public static void ShouldShowEditorPostfix() {
            DummyPlugin.Instance.Logger.Information("--- MainMenu::ShouldShowEditor::Postfix patch from dummy called: " + DummyPlugin.Instance?.IsEnabled);
        }
    }
}
