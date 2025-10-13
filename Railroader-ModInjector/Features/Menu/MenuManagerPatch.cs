using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Serilog;
using TMPro;
using UI.Common;
using UI.Menu;
using UnityEngine;

namespace Railroader.ModInjector.Features.Menu;

[HarmonyPatch]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal static class MenuManagerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MenuManager), "MakeMainMenu")]
    private static void Postfix(ref MainMenu __result, NavigationController ___navigationController, CreditsMenu ___creditsMenu) {
        Log.ForContext("SourceContext", "Railroader.ModInjector").Information("Patching MenuManager::Postfix");
        var original = __result.OnMainMenuAction;
        __result.OnMainMenuAction = action => {
            if (action == (MainMenu.MainMenuAction)(-1)) {
                ___navigationController.Push(CreateModsMenu(___creditsMenu));
                //___navigationController.Push(UnityEngine.Object.Instantiate(___creditsMenu)!);
                return;
            }

            original?.Invoke(action);
        };
    }

    private static ModsMenu CreateModsMenu(CreditsMenu creditsMenu) {
        var logger = Log.ForContext("SourceContext", "Railroader.ModInjector");

        var modsMenuObj = Object.Instantiate(creditsMenu.gameObject);
        modsMenuObj.SetActive(false);
        modsMenuObj.name = "ModsMenu";

        // Remove CreditsMenuScript
        var creditsMenuScript = modsMenuObj.GetComponent<CreditsMenu>();
        if (creditsMenuScript != null) {
            Object.DestroyImmediate(creditsMenuScript);
        } else {
            logger.Warning("CreditsMenuScript not found on cloned object");
        }

        // Add ModsMenu script
        var modsMenu = modsMenuObj.AddComponent<ModsMenu>();
        if (modsMenu == null) {
            logger.Error("Failed to add ModsMenu script");
            Object.Destroy(modsMenuObj);
            return null;
        }

        // Copy serialized fields from CreditsMenu (e.g., UIBuilderAssets)
        var fields = typeof(CreditsMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields) {
            var value = field.GetValue(creditsMenu);
            if (value != null) {
                field.SetValue(modsMenu, value);
                logger.Information($"Copied field {field.Name} to ModsMenu");
            }
        }

        // Update Menu Title text (optional, may be overridden by BuildPanelContent)
        var titleText = modsMenuObj.transform.Find("Menu Title")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null) {
            titleText.text = "Mods";
        } else {
            logger.Warning("Menu Title TextMeshProUGUI not found");
        }

        // Ensure the GameObject is active
        modsMenuObj.SetActive(true);

        logger.Information($"ModsMenu created: Components={modsMenuObj.GetComponents<Component>().Length}, Children={modsMenuObj.transform.childCount}");
        return modsMenu;
    }
}
