using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Railroader.ModManager.Behaviors;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Services;
using Serilog;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace Railroader.ModManager.Features;

public static class Bootstrapper
{
    public static IReadOnlyList<ModDefinition> ModDefinitions { get; internal set; } = [];

    [ExcludeFromCodeCoverage]
    public static void Execute() {
        var memoryLogger = new MemoryLogger();
        Log.Logger = memoryLogger;

        Execute(
            ModExtractor.ExtractMods(memoryLogger),
            ModDefinitionLoader.Factory(memoryLogger),
            Harmony.Factory("Railroader.ModManager"),
            CreateManagerBehaviour
        );
    }

    public static void Execute(ExtractModsDelegate extractMods, ModDefinitionLoaderDelegate modDefinitionLoader, IHarmony factory, Action createManagerBehaviour) {
        extractMods();
        ModDefinitions = modDefinitionLoader();

        factory.PatchCategory(typeof(ModManager).Assembly, "LogManager");

        createManagerBehaviour();
    }

    [ExcludeFromCodeCoverage]
    private static void CreateManagerBehaviour() {
        var gameObject = new GameObject("ModManager");
        gameObject.SetActive(false);
        gameObject.AddComponent<ManagerBehaviour>();
        gameObject.SetActive(true);
    }

    [ExcludeFromCodeCoverage]
    public static void LoadMods() =>
        LoadMods(
            Log.Logger.ForSourceContext(),
            ModDefinitions,
            ModDefinitionValidator.Factory,
            CodeCompiler.Factory(),
            CodePatcher.Factory(),
            PluginManager.Factory,
            Harmony.Factory("Railroader.ModManager")
        );

    public static void LoadMods(
        ILogger logger, 
        IReadOnlyList<ModDefinition> modDefinitions, 
        ModDefinitionValidatorDelegate modDefinitionValidator,
        CompileModDelegate codeCompiler,
        ApplyPatchesDelegate codePatcher,
        CreatePluginsDelegateFactory createPluginsDelegateFactory,
        IHarmony harmony
        ) {
        if (modDefinitions.Count == 0) {
            logger.Information("No mods where found.");
            return;
        }

        logger.Information("Validating mods ...");
        modDefinitions = modDefinitionValidator(modDefinitions);

        if (modDefinitions.Count == 0) {
            logger.Error("Validation error detected. Canceling mod loading.");
            return;
        }
        
        var mods = new Mod[modDefinitions.Count];

        for (var i = 0; i < modDefinitions.Count; i++) {
            var definition    = modDefinitions[i]!;
            var result = codeCompiler(definition);
            if (result == CompileModResult.Success) {
                if (codePatcher(definition) == false) {
                    result = CompileModResult.Error;
                }
            }

            var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
            mods[i] = new Mod(logger, definition, assemblyPath) {
                IsLoaded = result != CompileModResult.Error
            };
        }

        var moddingContext = new ModdingContext(mods);
        logger.Information("Created modding context ...");
        logger.Debug("mods: {mods}", JsonConvert.SerializeObject(moddingContext.Mods));

        var pluginManager = createPluginsDelegateFactory(moddingContext);

        logger.Information("Instantiating plugins ...");
        foreach (var mod in mods.Where(o => o.AssemblyPath != null)) {
            mod.Plugins = pluginManager(mod).ToArray();
            mod.IsLoaded = true;
        }

        foreach (var mod in mods) {
            logger.Debug("enabling mod {id}", mod.Definition.Identifier);
            mod.IsEnabled = true;
        }

        logger.Information("Applying harmony patches ...");

        harmony.PatchAllUncategorized(typeof(ModManager).Assembly);

        logger.Information("Mod loader loaded ...");
    }
}
