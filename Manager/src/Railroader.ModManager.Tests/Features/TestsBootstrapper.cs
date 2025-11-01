using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Features;
using Railroader.ModManager.Features.CodePatchers;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Tests.TestExtensions;
using Serilog;
using Serilog.Events;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsBootstrapper
{
    [DebuggerStepThrough]
    private static ExtractModsDelegate ExtractMods() => Substitute.For<ExtractModsDelegate>();

    [DebuggerStepThrough]
    private static ModDefinitionLoaderDelegate ModDefinitionLoader(ModDefinition[]? modDefinitions = null) {
        var mock = Substitute.For<ModDefinitionLoaderDelegate>();
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
    private static ModDefinitionValidatorDelegate Processor(ModDefinition[]? modDefinitions = null) {
        var mock = Substitute.For<ModDefinitionValidatorDelegate>();
        mock.Invoke(Arg.Any<IReadOnlyList<ModDefinition>>()).Returns(_ => modDefinitions);
        return mock;
    }

    [DebuggerStepThrough]
    private static CompileModDelegate Compiler(CompileModResult result = CompileModResult.Success) {
        var mock = Substitute.For<CompileModDelegate>();
        mock.Invoke(Arg.Any<ModDefinition>(), Arg.Any<string[]>()).Returns(result);
        return mock;
    }

    [DebuggerStepThrough]
    private static ApplyPatchesDelegate Patcher(bool result = true) {
        var mock = Substitute.For<ApplyPatchesDelegate>();
        mock.Invoke(Arg.Any<ModDefinition>(), Arg.Any<TypePatcherInfo[]>()).Returns(_ => result);
        return mock;
    }

    [DebuggerStepThrough]
    private static CreatePluginsDelegateFactory PluginFactory(CreatePluginsDelegate? createPluginsDelegate = null) {
        var mock = Substitute.For<CreatePluginsDelegateFactory>();
        mock.Invoke(Arg.Any<IModdingContext>()).Returns(_ => createPluginsDelegate ?? CreatePlugins());
        return mock;
    }

    [DebuggerStepThrough]
    private static CreatePluginsDelegate CreatePlugins() {
        var mock = Substitute.For<CreatePluginsDelegate>();
        mock.Invoke(Arg.Any<Mod>()).Returns([]);
        return mock;
    }

    private static readonly ModDefinition _ModDefinition = new() {
        Identifier = "Identifier",
        Name = "Name",
        Version = new Version(1,0),
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
        extractMods.ShouldReceiveOnly(o => o.Invoke());
    }

    [Fact]
    public void Execute_Calls_ModDefinitionLoader() {
        // Arrange
        ModDefinition[] modDefinitions      = [_ModDefinition];
        var             modDefinitionLoader = ModDefinitionLoader(modDefinitions);

        // Act
        Bootstrapper.Execute(ExtractMods(), modDefinitionLoader, Harmony(), CreateManagerBehaviour());

        // Assert
        modDefinitionLoader.ShouldReceiveOnly(o => o.Invoke());
        Bootstrapper.ModDefinitions.Should().BeEquivalentTo(modDefinitions);
    }

    [Fact]
    public void Execute_Calls_Harmony() {
        // Arrange
        var harmony = Harmony();

        // Act
        Bootstrapper.Execute(ExtractMods(), ModDefinitionLoader(), harmony, CreateManagerBehaviour());

        // Assert
        harmony.ShouldReceiveOnly(o => o.PatchCategory(typeof(ModManager).Assembly, "LogManager"));
    }

    [Fact]
    public void Execute_Calls_CreateManagerBehaviour() {
        // Arrange
        var createManagerBehaviour = CreateManagerBehaviour();

        // Act
        Bootstrapper.Execute(ExtractMods(), ModDefinitionLoader(), Harmony(), createManagerBehaviour);

        // Assert
        createManagerBehaviour.ShouldReceiveOnly(o => o.Invoke());
    }

    [Fact]
    public void LoadMods_When_No_Mods_Should_Log_And_Return() {
        // Arrange
        var logger = Logger();

        // Act
        Bootstrapper.LoadMods(logger, [], Processor(), Compiler(), Patcher(), PluginFactory(), Harmony());

        // Assert
        logger.ShouldReceiveOnly(o => o.Information("No mods where found."));
    }

    [Fact]
    public void LoadMods_When_Validation_Fails_Should_Log_Error_And_Cancel()
    {
        // Arrange
        var logger = Logger();

        var processor = Processor([]);
        
        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], processor, Compiler(), Patcher(), PluginFactory(), Harmony());

        // Assert
        logger.ShouldReceiveOnly(o => {
            o.Information("Validating mods ...");
            o.Error("Validation error detected. Canceling mod loading.");
        });
    }

    [Fact]
    public void LoadMods_Should_TryCompile_Each_Valid_Mod() {
        // Arrange
        var compiler = Compiler(CompileModResult.Error);
        var patcher  = Patcher();

        // Act
        Bootstrapper.LoadMods(Logger(), [_ModDefinition], Processor([_ModDefinition]), compiler, patcher, PluginFactory(), Harmony());

        // Assert
        compiler.ShouldReceiveOnly(o => o.Invoke(_ModDefinition));
        patcher.ShouldReceiveNoCalls();
    }

    [Fact]
    public void LoadMods_Should_TryPatch_Each_Compiled_Mod() {
        // Arrange
        var logger   = Logger();
        var compiler = Compiler();
        var patcher  = Patcher(false);

        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], Processor([_ModDefinition]), compiler, patcher, PluginFactory(), Harmony());

        // Assert
        compiler.ShouldReceiveOnly(o => o.Invoke(_ModDefinition));
        patcher.ShouldReceiveOnly(o => o.Invoke(_ModDefinition));

        logger.Received().Debug("mods: {mods}", """[{"Definition":{"id":"Identifier","name":"Name","version":"1.0","logLevel":"Debug","requires":null,"conflictsWith":null},"AssemblyPath":null,"IsEnabled":false,"IsValid":false,"IsLoaded":false,"Plugins":null}]""");
    }

    [Fact]
    public void LoadMods_Calls_PluginFactory() {
        // Arrange
        var logger        = Logger();
        var pluginFactory = PluginFactory();
        
        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], Processor([_ModDefinition]), Compiler(), Patcher(), pluginFactory, Harmony());

        // Assert
        pluginFactory.Received().Invoke(
            Arg.Do<IModdingContext>(o => {
                o.Mods.Should().HaveCount(1);
                var mod = o.Mods.First().Should().BeOfType<Mod>().Which;
                mod.Definition.Should().Be(_ModDefinition);
                mod.IsValid.Should().BeTrue();
                mod.AssemblyPath.Should().Be(@"BasePath\Identifier.dll");
            }));

    }

    [Fact]
    public void LoadMods_Calls_TryInstantiatePlugins() {
        // Arrange
        var logger                = Logger();
        var createPluginsDelegate = Substitute.For<CreatePluginsDelegate>();
        var plugin                = Substitute.For<IPlugin>();
        Mod? mod                   = null;
        createPluginsDelegate.Invoke(Arg.Any<Mod>()).Returns([plugin]).AndDoes(o => mod = o.Arg<Mod>());
        var pluginFactory = PluginFactory(createPluginsDelegate);
        
        // Act
        Bootstrapper.LoadMods(logger, [_ModDefinition], Processor([_ModDefinition]), Compiler(), Patcher(), pluginFactory, Harmony());

        // Assert
        mod.Should().NotBeNull();
        mod.IsLoaded.Should().BeTrue();
        mod.Plugins.Should().BeEquivalentTo([plugin]);
    }
}
