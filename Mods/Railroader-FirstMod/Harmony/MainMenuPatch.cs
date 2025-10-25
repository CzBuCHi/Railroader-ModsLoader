using HarmonyLib;
using JetBrains.Annotations;
using UI.Menu;

namespace Railroader.FirstMod.Harmony
{
    [HarmonyPatch]
    [UsedImplicitly]
    public static class MainMenuPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenu), "ShouldShowEditor")]
        public static bool ShouldShowEditorPrefix() {
            FirstPlugin.Instance.Logger.Information("--- MainMenu::ShouldShowEditor::Prefix patch from dummy called: " + FirstPlugin.Instance?.IsEnabled);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "ShouldShowEditor")]
        public static void ShouldShowEditorPostfix() {
            FirstPlugin.Instance.Logger.Information("--- MainMenu::ShouldShowEditor::Postfix patch from dummy called: " + FirstPlugin.Instance?.IsEnabled);
        }
    }
}
