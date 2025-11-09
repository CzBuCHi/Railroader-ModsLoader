using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Features;
using Railroader.ModManager.Features.CodePatchers;
using Railroader.ModManager.Interfaces;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsBootstrapper
{
    [DebuggerStepThrough]
    private static ModExtractionAction ExtractMods() => Substitute.For<ModExtractionAction>();

    [DebuggerStepThrough]
    private static LoadDefinitionsDelegate ModDefinitionLoader(ModDefinition[]? modDefinitions = null) {
        var mock = Substitute.For<LoadDefinitionsDelegate>();
        mock.Invoke().Returns(_ => modDefinitions ?? []);
        return mock;
    }

    [DebuggerStepThrough]
    private static IHarmony Harmony() => Substitute.For<IHarmony>();

    [DebuggerStepThrough]
    private static Action CreateManagerBehaviour() => Substitute.For<Action>();

    [DebuggerStepThrough]
    private static ILogger Logger() => Substitute.For<ILogger>();

    [DebuggerStepThrough]
    private static ValidateMods Processor(ModDefinition[]? modDefinitions = null) {
        var mock = Substitute.For<ValidateMods>();
        mock.Invoke(Arg.Any<IReadOnlyList<ModDefinition>>()).Returns(_ => modDefinitions);
        return mock;
    }

    [DebuggerStepThrough]
    private static CompileModAction Compiler(CompileModResult result = CompileModResult.Success) {
        var mock = Substitute.For<CompileModAction>();
        mock.Invoke(Arg.Any<ModDefinition>(), Arg.Any<string[]>()).Returns(result);
        return mock;
    }

    [DebuggerStepThrough]
    private static PatchModAction Patcher(bool result = true) {
        var mock = Substitute.For<PatchModAction>();
        mock.Invoke(Arg.Any<ModDefinition>(), Arg.Any<TypePatcherInfo[]>()).Returns(_ => result);
        return mock;
    }

    [DebuggerStepThrough]
    private static PluginLoaderFactory PluginFactory(PluginLoader? createPluginsDelegate = null) {
        var mock = Substitute.For<PluginLoaderFactory>();
        mock.Invoke(Arg.Any<IModdingContext>()).Returns(_ => createPluginsDelegate ?? CreatePlugins());
        return mock;
    }

    [DebuggerStepThrough]
    private static PluginLoader CreatePlugins() {
        var mock = Substitute.For<PluginLoader>();
        mock.Invoke(Arg.Any<Mod>()).Returns([]);
        return mock;
    }
    
    private static readonly ModDefinition _ModDefinition = new() {
        Identifier = "Identifier",
        Name = "Name",
        Version = new Version(1, 0),
        LogLevel = LogEventLevel.Debug,
        BasePath = "BasePath"
    };

    [Fact]
    public void Execute_Calls_ExtractMods() {
        // Arrange 
        var extractMods = ExtractMods();

        // Act
        Bootstrapper.Execute(extractMods, ModDefinitionLoader(), Harmony(), CreateManagerBehaviour());

        // Assert
        extractMods.Received().Invoke();
    }

    [Fact]
    public void Execute_Calls_ModDefinitionLoader() {
        // Arrange
        ModDefinition[] modDefinitions      = [_ModDefinition];
        var             modDefinitionLoader = ModDefinitionLoader(modDefinitions);

        // Act
        Bootstrapper.Execute(ExtractMods(), modDefinitionLoader, Harmony(), CreateManagerBehaviour());

        // Assert
        modDefinitionLoader.Received().Invoke();
        Bootstrapper.ModDefinitions.ShouldBeEquivalentTo(modDefinitions);
    }

    [Fact]
    public void Execute_Calls_Harmony() {
        // Arrange
        var harmony = Harmony();

        // Act
        Bootstrapper.Execute(ExtractMods(), ModDefinitionLoader(), harmony, CreateManagerBehaviour());

        // Assert
        harmony.Received().PatchCategory(typeof(ModManager).Assembly, "LogManager");
    }

    [Fact]
    public void Execute_Calls_CreateManagerBehaviour() {
        // Arrange
        var createManagerBehaviour = CreateManagerBehaviour();

        // Act
        Bootstrapper.Execute(ExtractMods(), ModDefinitionLoader(), Harmony(), createManagerBehaviour);

        // Assert
        createManagerBehaviour.Received().Invoke();
    }

    [Fact]
    public void LoadMods_When_No_Mods_Should_Log_And_Return() {
        // Arrange
        var logger = Logger();

        // Act
        Bootstrapper.LoadMods(logger, [], Processor(), Compiler(), Patcher(), PluginFactory(), Harmony());

        // Assert
        logger.Received().Information("No mods where found.");
    }

    [Fact]
    public void LoadMods_When_Validation_Fails_Should_Log_Error_And_Cancel() {
        // Arrange
        var logger = Logger();

        var processor = Processor([]);

        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], processor, Compiler(), Patcher(), PluginFactory(), Harmony());

        // Assert
        logger.Received().Information("Validating mods ...");
        logger.Received().Error("Validation error detected. Canceling mod loading.");
        logger.DidNotReceive().Information("Created modding context ...");
    }

    [Fact]
    public void LoadMods_Should_TryCompile_Each_Valid_Mod() {
        // Arrange
        var compiler = Compiler(CompileModResult.Error);
        var patcher  = Patcher();

        // Act
        Bootstrapper.LoadMods(Logger(), [_ModDefinition], Processor([_ModDefinition]), compiler, patcher, PluginFactory(), Harmony());

        // Assert
        compiler.Received().Invoke(_ModDefinition);
        patcher.ShouldReceiveNoCalls();
    }

    [Fact]
    public void LoadMods_Should_TryPatch_Each_Valid_Mod() {
        // Arrange
        var patcher       = Patcher(false);
        var createPlugins = CreatePlugins();


        // Act
        
        Bootstrapper.LoadMods(Logger(), [_ModDefinition], Processor([_ModDefinition]), Compiler(), patcher, PluginFactory(createPlugins), Harmony());

        // Assert
        patcher.Received().Invoke(Arg.Any<ModDefinition>(), Arg.Any<TypePatcherInfo[]>());
        createPlugins.DidNotReceive().Invoke(Arg.Any<Mod>());
    }
    
    [Fact]
    [ExcludeFromCodeCoverage] 
    public void LoadMods_Calls_PluginFactory() {
        // Arrange
        var logger        = Logger();
        var pluginFactory = PluginFactory();

        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], Processor([_ModDefinition]), Compiler(), Patcher(), pluginFactory, Harmony());

        // Assert
        logger.Received().Debug("mods: {mods}", """[{"Definition":{"id":"Identifier","name":"Name","version":"1.0","logLevel":"Debug","requires":{},"conflictsWith":{},"resources":{}},"AssemblyPath":"BasePath\\Identifier.dll","IsEnabled":false,"IsValid":true,"IsLoaded":false,"Plugins":null}]"""); 
        
        logger.Received().Information("Created modding context ...");
        logger.Received().Information("Instantiating plugins ...");

        pluginFactory.Received().Invoke(
            Arg.Do<IModdingContext>(o => {
                o.Mods.Count.ShouldBe(1);
                var mod = o.Mods.First().ShouldBeOfType<Mod>();
                mod.Definition.ShouldBe(_ModDefinition);
                mod.IsValid.ShouldBeTrue();
                mod.AssemblyPath.ShouldBe(@"BasePath\Identifier.dll");
            }));
    }

    [Fact]
    public void LoadMods_Calls_Harmony() {
        // Arrange
        var logger  = Logger();
        var harmony = Harmony();

        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], Processor([_ModDefinition]), Compiler(), Patcher(), PluginFactory(), harmony);

        // Assert
        logger.Received().Information("Applying harmony patches ...");
        harmony.Received().PatchAllUncategorized(typeof(ModManager).Assembly);
    }

    [Fact]
    public void LoadMods_Calls_TryInstantiatePlugins() {
        // Arrange
        var  logger                = Logger();
        var  createPluginsDelegate = Substitute.For<PluginLoader>();
        var  plugin                = Substitute.For<IPlugin>();
        Mod? mod                   = null;
        createPluginsDelegate.Invoke(Arg.Any<Mod>()).Returns([plugin]).AndDoes(o => mod = o.Arg<Mod>());
        var pluginFactory = PluginFactory(createPluginsDelegate);

        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], Processor([_ModDefinition]), Compiler(), Patcher(), pluginFactory, Harmony());

        // Assert
        mod.ShouldNotBeNull();
        mod.IsLoaded.ShouldBeTrue();
        mod.Plugins.ShouldBeEquivalentTo(new[] { plugin });
    }

    [Fact]
    public void LoadMods_InvalidCodeCompilerResult() {
        // Arrange
        var compiler = Compiler((CompileModResult)(-1));
        var patcher  = Patcher();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Bootstrapper.LoadMods(Logger(), [_ModDefinition], Processor([_ModDefinition]), compiler, patcher, PluginFactory(), Harmony()));
    }
}
