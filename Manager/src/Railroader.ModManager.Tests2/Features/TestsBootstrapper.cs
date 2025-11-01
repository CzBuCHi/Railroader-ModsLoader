using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Features;
using Railroader.ModManager.Features.CodePatchers;
using Railroader.ModManager.Tests.TestExtensions;
using Serilog;
using Serilog.Events;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsBootstrapper
{
    private static ExtractModsDelegate ExtractMods() => Substitute.For<ExtractModsDelegate>();

    private static ModDefinitionLoaderDelegate ModDefinitionLoader(ModDefinition[]? modDefinitions = null) {
        var mock = Substitute.For<ModDefinitionLoaderDelegate>();
        mock.Invoke().Returns(_ => modDefinitions ?? []);
        return mock;
    }

    private static IHarmony Harmony() => Substitute.For<IHarmony>();

    private static Action CreateManagerBehaviour() => Substitute.For<Action>();

    private static ILogger Logger() => Substitute.For<ILogger>();

    private static ModDefinitionValidatorDelegate Processor(ModDefinition[]? modDefinitions = null) {
        var mock = Substitute.For<ModDefinitionValidatorDelegate>();
        mock.Invoke(Arg.Any<IReadOnlyList<ModDefinition>>()).Returns(_ => modDefinitions);
        return mock;
    }

    private static CompileModDelegate Compiler(CompileModResult result = CompileModResult.Success) {
        var mock = Substitute.For<CompileModDelegate>();
        mock.Invoke(Arg.Any<ModDefinition>(), Arg.Any<string[]>()).Returns(result);
        return mock;
    }

    private static ApplyPatchesDelegate Patcher(bool result = true) {
        var mock = Substitute.For<ApplyPatchesDelegate>();
        mock.Invoke(Arg.Any<ModDefinition>(), Arg.Any<TypePatcherInfo[]>()).Returns(_ => result);
        return mock;
    }

    private static CreatePluginsDelegateFactory PluginFactory() => Substitute.For<CreatePluginsDelegateFactory>();

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

        var xx = logger.ReceivedCalls().ToString("logger.");
    }
}
