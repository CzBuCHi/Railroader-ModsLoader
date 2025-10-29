using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.CodePatchers;
using Railroader.ModManager.CodePatchers.Special;
using Railroader.ModManager.Interfaces;

namespace Railroader.ModManager.Tests.CodePatchers.Special;

public sealed class TestsHarmonyPluginPatcher
{
    [Fact]
    public void Constructor() {
        // Arrange
        var serviceManager = new TestServiceManager();

        var methodPatchersField = typeof(TypePatcher).GetField("_MethodPatchers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var type                = typeof(MethodPatcher<IHarmonyPlugin, HarmonyPluginPatcher>);
        var targetBaseTypeField = type.GetField("_TargetBaseType", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var loggerField         = type.GetField("_Logger", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var targetMethodField   = type.GetField("_TargetMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var injectedMethodField = type.GetField("_InjectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var injectedMethod      = typeof(HarmonyPluginPatcher).GetMethod("OnIsEnabledChanged", BindingFlags.Static | BindingFlags.Public)!;

        // Act
        var sut = serviceManager.CreateHarmonyPluginPatcher();

        // Assert
        var methodPatchers = methodPatchersField.GetValue(sut);
        var array          = methodPatchers.Should().BeOfType<IMethodPatcher[]>().Which;
        array.Should().HaveCount(1);
        var patcher = array[0].Should().BeOfType<MethodPatcher<IHarmonyPlugin, HarmonyPluginPatcher>>().Which;

        targetBaseTypeField.GetValue(patcher).Should().Be(typeof(PluginBase<>));
        loggerField.GetValue(patcher).Should().Be(serviceManager.ContextLogger);
        targetMethodField.GetValue(patcher).Should().Be("OnIsEnabledChanged");
        injectedMethodField.GetValue(patcher).Should().Be(injectedMethod);
    }

    [Fact]
    public void PatchAllWhenEnabled() {
        // Arrange
        var serviceManager = new TestServiceManager();
        
        // act
        var plugin = Substitute.For<IPlugin>();
        plugin.IsEnabled.Returns(true);
        plugin.Mod.Returns(new Mod(new ModDefinition { Identifier = "Identifier" }, null));

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        serviceManager.HarmonyWrapper.Received(1).PatchAll(plugin.GetType().Assembly);
        serviceManager.HarmonyWrapper.ReceivedCalls().Should().HaveCount(1);

        serviceManager.ContextLogger.Received().Information("Applying Harmony patch for mod {ModId}", "Identifier");
    }

    [Fact]
    public void UnpatchAllWhenDisabled() {
        // Arrange
        var serviceManager = new TestServiceManager();
        ModManager.ServiceProvider = serviceManager.ServiceProvider;

        var plugin = Substitute.For<IPlugin>();
        plugin.Mod.Returns(new Mod(new ModDefinition { Identifier = "Identifier" }, null));

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        serviceManager.HarmonyWrapper.Received(1).UnpatchAll("Identifier");
        serviceManager.HarmonyWrapper.ReceivedCalls().Should().HaveCount(1);

        serviceManager.ContextLogger.Received().Information("Removing Harmony patch for mod {ModId}", "Identifier");
    }

    [Fact]
    public void IgnoreRepeatCalls() {
        // Arrange
        var serviceManager = new TestServiceManager();
        ModManager.ServiceProvider = serviceManager.ServiceProvider;
        
        var plugin = Substitute.For<IPlugin>();
        plugin.IsEnabled.Returns(true);
        plugin.Mod.Returns(new Mod(new ModDefinition { Identifier = "Identifier" }, null));

        // Act
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);
        HarmonyPluginPatcher.OnIsEnabledChanged(plugin);

        // Assert
        serviceManager.HarmonyWrapper.Received(1).PatchAll(plugin.GetType().Assembly);
        serviceManager.HarmonyWrapper.ReceivedCalls().Should().HaveCount(1);

        serviceManager.ContextLogger.Received(1).Information("Applying Harmony patch for mod {ModId}", "Identifier");
    }
}
