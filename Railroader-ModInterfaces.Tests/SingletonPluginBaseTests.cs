using System;
using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInterfaces;

namespace Railroader_ModInterfaces.Tests;

public sealed class SingletonPluginBaseTests
{
    [Fact]
    public void ThrowsOnSecondConstructorCall() {
        // Arrange
        var moddingContext = Substitute.For<IModdingContext>();
        var modDefinition  = Substitute.For<IModDefinition>();
        var sut            = new TestPlugin(moddingContext, modDefinition);

        // Act
        var act = () => new TestPlugin(moddingContext, modDefinition);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"Cannot create singleton plugin '{typeof(TestPlugin)}' twice.");
        TestPlugin.Instance.Should().Be(sut);

        typeof(SingletonPluginBase<TestPlugin>)
            .GetField("_Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null!, null!);
    }


    private sealed class TestPlugin(IModdingContext moddingContext, IModDefinition modDefinition) : SingletonPluginBase<TestPlugin>(moddingContext, modDefinition);
}
