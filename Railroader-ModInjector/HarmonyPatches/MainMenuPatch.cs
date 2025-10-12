using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Serilog;
using UI.Menu;

namespace Railroader.ModInjector.HarmonyPatches;

[HarmonyPatch]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal static class MainMenuPatch
{
    private static readonly ILogger _Logger = ModLogger.ForContext(typeof(MainMenuPatch));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MainMenu), "Awake")]
    public static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions) {
        _Logger.Information("Patching MainMenu::Awake");

        var codeInstructions = instructions.ToList();
        var injected         = false;

        for (var i = 0; i < codeInstructions.Count; i++) {
            if (codeInstructions[i]!.opcode != OpCodes.Ldstr || 
                codeInstructions[i]!.operand?.ToString() != "Help") {
                continue;
            }

            var newInstructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(MainMenuPatch).GetMethod(nameof(InjectedButton), BindingFlags.Static | BindingFlags.NonPublic)!)
            };

            // Insert the new instructions
            codeInstructions.InsertRange(i, newInstructions);

            injected = true;
            break;
        }

        if (!injected) {
            _Logger.Error("Failed to patch method MainMenu::Awake");
        }

        return codeInstructions;
    }

    private static void InjectedButton(MainMenu mainMenu) {
        mainMenu.AddButton("Mods", () => {
            _Logger.Information("Mods button clicked");
        });
    }
}
