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
        var sut            = new TestPlugin(moddingContext);

        // Act
        var act = () => new TestPlugin(moddingContext);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"Cannot create singleton plugin '{typeof(TestPlugin)}' twice.");
        TestPlugin.Instance.Should().Be(sut);

        typeof(SingletonPluginBase<TestPlugin>)
            .GetProperty(nameof(SingletonPluginBase<TestPlugin>.Instance), BindingFlags.Static | BindingFlags.Public)!
            .SetValue(null!, null!);
    }


    private sealed class TestPlugin(IModdingContext moddingContext) : SingletonPluginBase<TestPlugin>(moddingContext);
}
