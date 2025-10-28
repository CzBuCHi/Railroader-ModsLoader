using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

namespace Railroader.ModInjector.Services;

/// <summary> Manages the lifecycle of all loaded mods. </summary>
internal interface IModManager
{
    /// <summary> Initializes the mod system. </summary>
    /// <param name="modDefinitions"></param>
    void Bootstrap(ModDefinition[] modDefinitions);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage] // TODO: remove this
internal sealed class ModManager : IModManager
{
    public delegate IPluginManager PluginManagerFactoryDelegate(ModdingContext moddingContext);

    public required ICodeCompiler                CodeCompiler           { get; init; }
    public required PluginManagerFactoryDelegate PluginManagerFactory   { get; init; }
    public required IModDefinitionProcessor      ModDefinitionProcessor { get; init; }
    public required IModExtractorService         ModExtractorService    { get; init; }

    private Mod[] _Mods = null!;
    
    /// <inheritdoc />
    public void Bootstrap(ModDefinition[] modDefinitions) {
        var logger = DI.GetLogger();
        logger.Debug("Bootstrap start");
        ModExtractorService.ExtractMods();

        if (!ModDefinitionProcessor.PreprocessModDefinitions(ref modDefinitions)) {
            return;
        }

        _Mods = new Mod[modDefinitions.Length];

        for (var i = 0; i < modDefinitions.Length; i++) {
            var definition    = modDefinitions[i];
            var outputDllPath = CodeCompiler.CompileMod(definition);
            _Mods[i] = new Mod(definition, outputDllPath);
        }

        var moddingContext = new ModdingContext(_Mods);

        logger.Debug("mods: {mods}", JsonConvert.SerializeObject(moddingContext.Mods));

        var pluginManger = PluginManagerFactory(moddingContext);
        foreach (var mod in _Mods.Where(o => o.AssemblyPath != null)) {
            mod.Plugins = pluginManger.CreatePlugins(mod).ToArray();
            mod.IsLoaded = true;
        }

        foreach (var mod in _Mods) {
            logger.Debug("enabling mod {id}", mod.Definition.Identifier);
            mod.IsEnabled = true;
        }

        logger.Information("Applying harmony patches ...");
        var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
        harmony.PatchAll(typeof(Injector).Assembly);

        logger.Debug("Bootstrap complete");
    }
}
