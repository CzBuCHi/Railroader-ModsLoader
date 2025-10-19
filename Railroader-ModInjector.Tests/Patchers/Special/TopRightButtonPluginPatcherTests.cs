using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector.Patchers;
using Railroader.ModInjector.Patchers.Special;
using Railroader.ModInterfaces;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace Railroader_ModInterfaces.Tests.Patchers.Special;

public sealed class TopRightButtonPluginPatcherTests
{
    [Fact]
    public void Constructor() {
        // Arrange
        var logger              = Substitute.For<ILogger>();
        var methodPatchersField = typeof(TypePatcher).GetField("<methodPatchers>P", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var type                = typeof(MethodPatcher<ITopRightButtonPlugin, TopRightButtonPluginPatcher>);
        var targetBaseTypeField = type.GetField("_TargetBaseType", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var loggerField         = type.GetField("_Logger", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var targetMethodField   = type.GetField("_TargetMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var injectedMethodField = type.GetField("_InjectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var injectedMethod      = typeof(TopRightButtonPluginPatcher).GetMethod("OnIsEnabledChanged", BindingFlags.Static | BindingFlags.Public)!;

        // Act
        var sut = new TopRightButtonPluginPatcher(logger);

        // Assert
        var methodPatchers = methodPatchersField.GetValue(sut);
        var array          = methodPatchers.Should().BeOfType<IMethodPatcher[]>().Which;
        array.Should().HaveCount(1);
        var patcher = array[0].Should().BeOfType<MethodPatcher<ITopRightButtonPlugin, TopRightButtonPluginPatcher>>().Which;

        targetBaseTypeField.GetValue(patcher).Should().Be(typeof(PluginBase<>));
        loggerField.GetValue(patcher).Should().Be(logger);
        targetMethodField.GetValue(patcher).Should().Be("OnIsEnabledChanged");
        injectedMethodField.GetValue(patcher).Should().Be(injectedMethod);
    }
}
