using System.Linq;
using Newtonsoft.Json;
using Railroader.ModInjector.Wrappers;
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
internal sealed class ModManager : IModManager
{
    public ICodeCompiler?  CodeCompiler  { get; set; }
    public IPluginManager? PluginManager { get; set; }

    private        Mod[]   _Mods   = null!;
    private static ILogger _Logger = null!;

    /// <inheritdoc />
    public void Bootstrap(ModDefinition[] modDefinitions) {
        _Logger = Log.ForContext("SourceContext", "Railroader.ModInjector");
        _Logger.Debug("Bootstrap start");

        CodeCompiler ??= new CodeCompiler(new FileSystemWrapper(_Logger), new CompilerCallableEntryPointWrapper(), _Logger);

        _Mods = new Mod[modDefinitions.Length];

        for (var i = 0; i < modDefinitions.Length; i++) {
            var definition    = modDefinitions[i];
            var outputDllPath = CodeCompiler.CompileMod(definition);
            _Mods[i] = new Mod(definition, outputDllPath);
        }

        var moddingContext = new ModdingContext(_Mods);

        _Logger.Debug("moddingContext: {moddingContext}", JsonConvert.SerializeObject(moddingContext));

        PluginManager ??= new PluginManager(moddingContext);
        foreach (var mod in _Mods.Where(o => o.OutputDllPath != null)) {
            mod.Plugins = PluginManager.CreatePlugins(mod).ToArray();
            mod.IsLoaded = true;
        }

        foreach (var mod in _Mods) {
            _Logger.Debug("enabling mod {id}", mod.Definition.Identifier);
            mod.IsEnabled = true;
        }

        _Logger.Information("Applying harmony patches ...");
        var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
        harmony.PatchAll(typeof(Injector).Assembly);

        _Logger.Debug("Bootstrap complete");
    }
}
