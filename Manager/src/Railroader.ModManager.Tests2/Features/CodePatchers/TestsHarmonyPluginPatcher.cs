using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Features.CodePatchers;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Tests.Features.CodePatchers;

public sealed class TestsHarmonyPluginPatcher
{
    [Fact]
    public void Factory() {
        // Arrange
        var logger = Substitute.For<ILogger>();

        const string source = """
                              using Railroader.ModManager.Tests.Features.CodePatchers;

                              namespace Foo.Bar { 
                                  public class TargetType : BaseType{ }
                              }
                              """;

        var (assemblyDefinition, _) = TestUtils.BuildAssemblyDefinition(source);
        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

        // Act
        var harmonyPluginPatcher = HarmonyPluginPatcher.Factory(logger);
        harmonyPluginPatcher(assemblyDefinition, typeDefinition).Should().BeFalse();

        // Assert
        logger.Debug("Skipping patching for type {TypeName}: not derived from {BaseType} or does not implement {MarkerInterface}", typeDefinition.FullName, typeof(HarmonyPluginPatcher), typeof(IMarker));
    }

    [Fact]
    public void PatchAllWhenEnabled() {
        // Arrange
        var logger         = Substitute.For<ILogger>();
        var harmony        = Substitute.For<IHarmony>();
        var moddingContext = new ModdingContext([], logger, _ => harmony);

        var plugin = Substitute.For<IHarmonyPlugin>();
        plugin.IsEnabled.Returns(true);
        plugin.Mod.Returns(new Mod(logger, new ModDefinition { Identifier = "Identifier", }, null));
        plugin.ModdingContext.Returns(moddingContext);

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        harmony.Received(1).PatchAll(plugin.GetType().Assembly);
        harmony.ReceivedCalls().Should().HaveCount(1);

        logger.Received().Information("Applying Harmony patch for mod {ModId}", "Identifier");
    }

    [Fact]
    public void UnpatchAllWhenDisabled() {
        // Arrange

        var logger         = Substitute.For<ILogger>();
        var harmony        = Substitute.For<IHarmony>();
        var moddingContext = new ModdingContext([], logger, _ => harmony);

        var plugin = Substitute.For<IHarmonyPlugin>();
        plugin.IsEnabled.Returns(false);
        plugin.Mod.Returns(new Mod(logger, new ModDefinition { Identifier = "Identifier", }, null));
        plugin.ModdingContext.Returns(moddingContext);

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        harmony.Received(1).UnpatchAll("Identifier");
        harmony.ReceivedCalls().Should().HaveCount(1);

        logger.Received().Information("Removing Harmony patch for mod {ModId}", "Identifier");
    }

    [Fact]
    public void IgnoreRepeatCalls() {
        // Arrange
        var logger         = Substitute.For<ILogger>();
        var harmony        = Substitute.For<IHarmony>();
        var moddingContext = new ModdingContext([], logger, _ => harmony);

        var plugin = Substitute.For<IHarmonyPlugin>();
        plugin.IsEnabled.Returns(true);
        plugin.Mod.Returns(new Mod(logger, new ModDefinition { Identifier = "Identifier", }, null));
        plugin.ModdingContext.Returns(moddingContext);

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        harmony.Received(1).PatchAll(plugin.GetType().Assembly);
        harmony.ReceivedCalls().Should().HaveCount(1);

        logger.Received(1).Information("Applying Harmony patch for mod {ModId}", "Identifier");
    }
}

