using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Analytics;
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
        var gameObject = new GameObject("ModManagerBootstrapper");
        gameObject.AddComponent<ManagerBootstrapperBehaviour>();
    }

    internal static void ExecuteCore() {
        var memoryLogger = new MemoryLogger();
        Log.Logger = memoryLogger;
       
        Execute(
            ModExtractor.GetExtractor(memoryLogger),
            ModDefinitionLoader.Create(memoryLogger),
            Harmony.Factory("Railroader.ModManager"),
            CreateManagerBehaviour
        );
    }

    public static void Execute(ModExtractionAction extractMods, LoadDefinitionsDelegate loadDefinitions, IHarmony factory, Action createManagerBehaviour) {
        extractMods();
        ModDefinitions = loadDefinitions();

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
            ModDefinitionValidator.Create,
            CodeCompiler.Factory(),
            CodePatcher.Create(),
            PluginManager.CreateLoader,
            Harmony.Factory("Railroader.ModManager")
        );

    internal static void LoadMods(
        ILogger logger, 
        IReadOnlyList<ModDefinition> modDefinitions, 
        ValidateMods modDefinitionValidator,
        CompileModDelegate codeCompiler,
        ApplyPatchesDelegate codePatcher,
        PluginLoaderFactory createPluginsDelegateFactory,
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

            var assemblyPath = result switch {
                CompileModResult.None or CompileModResult.Error      => null,
                CompileModResult.Success or CompileModResult.Skipped => Path.Combine(definition.BasePath, definition.Identifier + ".dll"),
                _                                                    => throw new ArgumentOutOfRangeException()
            };

            mods[i] = new Mod(logger, definition) {
                AssemblyPath = assemblyPath,
                IsValid = result != CompileModResult.Error
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

        logger.Information("Applying harmony patches ...");

        harmony.PatchAllUncategorized(typeof(ModManager).Assembly);
    }
}
