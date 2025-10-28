using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Patchers;
using Railroader.ModManager.Patchers.Special;
using Railroader.ModManager.Wrappers;
using Serilog;

namespace Railroader.ModManager.Tests.Patchers.Special;

public sealed class TestsHarmonyPluginPatcher
{
    [Fact]
    public void Constructor() {
        // Arrange
        var logger              = Substitute.For<ILogger>();
        var methodPatchersField = typeof(TypePatcher).GetField("<methodPatchers>P", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var type                = typeof(MethodPatcher<IHarmonyPlugin, HarmonyPluginPatcher>);
        var targetBaseTypeField = type.GetField("_TargetBaseType", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var loggerField         = type.GetField("_Logger", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var targetMethodField   = type.GetField("_TargetMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var injectedMethodField = type.GetField("_InjectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var injectedMethod      = typeof(HarmonyPluginPatcher).GetMethod("OnIsEnabledChanged", BindingFlags.Static | BindingFlags.Public)!;

        // Act
        var sut = new HarmonyPluginPatcher(logger);

        // Assert
        var methodPatchers = methodPatchersField.GetValue(sut);
        var array          = methodPatchers.Should().BeOfType<IMethodPatcher[]>().Which;
        array.Should().HaveCount(1);
        var patcher = array[0].Should().BeOfType<MethodPatcher<IHarmonyPlugin, HarmonyPluginPatcher>>().Which;

        targetBaseTypeField.GetValue(patcher).Should().Be(typeof(PluginBase<>));
        loggerField.GetValue(patcher).Should().Be(logger);
        targetMethodField.GetValue(patcher).Should().Be("OnIsEnabledChanged");
        injectedMethodField.GetValue(patcher).Should().Be(injectedMethod);
    }

    [Fact]
    public void PatchAllWhenEnabled() {
        // Arrange
        var harmony = Substitute.For<IHarmonyWrapper>();
        DI.HarmonyWrapper = _ => harmony;

        var logger  = Substitute.For<ILogger>();
        DI.Logger = logger;
        var getLoggerScope = "INITIAL";
        DI.GetLogger = scope => {
            getLoggerScope = scope;
            return logger;
        };

        // act
        var plugin = Substitute.For<IPlugin>();
        plugin.IsEnabled.Returns(true);
        plugin.Mod.Returns(new Mod(new ModDefinition { Identifier = "Identifier" }, null));

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        harmony.Received(1).PatchAll(plugin.GetType().Assembly);
        harmony.ReceivedCalls().Should().HaveCount(1);

        getLoggerScope.Should().BeNull();

        logger.Received().Information("Applying Harmony patch for mod {ModId}", "Identifier");
    }

    [Fact]
    public void UnpatchAllWhenDisabled() {
        // Arrange
        var harmony = Substitute.For<IHarmonyWrapper>();

        string? harmonyId = null;
        DI.HarmonyWrapper = id => {
            harmonyId = id;
            return harmony;
        };

        var logger  = Substitute.For<ILogger>();
        DI.Logger = logger;
        DI.GetLogger = _ => logger;

        var plugin = Substitute.For<IPlugin>();
        plugin.Mod.Returns(new Mod(new ModDefinition { Identifier = "Identifier" }, null));

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert

        harmonyId.Should().Be("Identifier");
        harmony.Received(1).UnpatchAll("Identifier");
        harmony.ReceivedCalls().Should().HaveCount(1);

        logger.Received().Information("Removing Harmony patch for mod {ModId}", "Identifier");
    }

    [Fact]
    public void IgnoreRepeatCalls() {
        // Arrange
        var harmony = Substitute.For<IHarmonyWrapper>();
        DI.HarmonyWrapper = _ => harmony;

        var plugin = Substitute.For<IPlugin>();
        plugin.IsEnabled.Returns(true);
        plugin.Mod.Returns(new Mod(new ModDefinition { Identifier = "Identifier" }, null));

        var logger  = Substitute.For<ILogger>();
        DI.Logger = logger;
        DI.GetLogger = _ => logger;

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        harmony.Received(1).PatchAll(plugin.GetType().Assembly);
        harmony.ReceivedCalls().Should().HaveCount(1);

        logger.Received(1).Information("Applying Harmony patch for mod {ModId}", "Identifier");
    }
}
