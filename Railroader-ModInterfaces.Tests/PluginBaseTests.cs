using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInterfaces;

namespace Railroader_ModInterfaces.Tests;

public sealed class PluginBaseTests
{
    [Fact]
    public void CallsOnEnableCorrectly() {
        // Arrange
        var moddingContext = Substitute.For<IModdingContext>();
        var modDefinition  = Substitute.For<IModDefinition>();
        var sut            = new TestPlugin(moddingContext, modDefinition);
        sut.SetIsEnabled(false);

        // Act
        sut.IsEnabled = true;
        sut.IsEnabled = true;

        // Assert
        sut.IsEnabled.Should().BeTrue();
        sut.OnEnableCalls.Should().Be(1);
        sut.OnDisableCalls.Should().Be(0);
    }

    [Fact]
    public void CallsOnDisableCorrectly() {
        // Arrange
        var moddingContext = Substitute.For<IModdingContext>();
        var modDefinition  = Substitute.For<IModDefinition>();
        var sut            = new TestPlugin(moddingContext, modDefinition);
        sut.SetIsEnabled(true);

        // Act
        sut.IsEnabled = false;
        sut.IsEnabled = false;

        // Assert
        sut.IsEnabled.Should().BeFalse();
        sut.OnEnableCalls.Should().Be(0);
        sut.OnDisableCalls.Should().Be(1);
    }

    private sealed class TestPlugin(IModdingContext moddingContext, IModDefinition modDefinition) : PluginBase(moddingContext, modDefinition)
    {
        private readonly FieldInfo _IsEnabled = typeof(PluginBase).GetField("_IsEnabled", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public void SetIsEnabled(bool value) {
            _IsEnabled.SetValue(this, value);
        }

        public int OnEnableCalls;

        public override void OnEnable() {
            base.OnEnable();
            OnEnableCalls++;
        }

        public int OnDisableCalls;

        public override void OnDisable() {
            base.OnDisable();
            OnDisableCalls++;
        }
    }
}
