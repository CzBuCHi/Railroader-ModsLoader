using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector;
using Railroader.ModInjector.Patchers;
using Railroader.ModInjector.Patchers.Special;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader_ModInterfaces.Tests.Patchers.Special;

public sealed class HarmonyPluginPatcherTests
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

        DI.HarmonyWrapper = s => harmony;

        var plugin = Substitute.For<IPluginBase>();
        plugin.IsEnabled.Returns(true);

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        harmony.Received(1).PatchAll(plugin.GetType().Assembly);
        harmony.ReceivedCalls().Should().HaveCount(1);
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

        var mod = new Mod(new ModDefinition { Identifier = "Identifier" }, null);

        var plugin = Substitute.For<IPluginBase>();
        plugin.Mod.Returns(mod);

        
        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert

        harmonyId.Should().Be("Identifier");
        harmony.Received(1).UnpatchAll("Identifier");
        harmony.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void IgnoreRepeatCalls() {
        // Arrange
        var harmony = Substitute.For<IHarmonyWrapper>();

        DI.HarmonyWrapper = s => harmony;

        var plugin = Substitute.For<IPluginBase>();
        plugin.IsEnabled.Returns(true);

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        harmony.Received(1).PatchAll(plugin.GetType().Assembly);
        harmony.ReceivedCalls().Should().HaveCount(1);
    }
}
