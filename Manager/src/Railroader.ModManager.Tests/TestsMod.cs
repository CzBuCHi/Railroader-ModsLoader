using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Tests;

public sealed class TestsMod
{
    [Fact]
    public void Constructor() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinition = Substitute.For<IModDefinition>();

        // Act
        var sut = new Mod(logger, modDefinition);

        // Assert
        sut.Should().NotBeNull();
        sut.Definition.Should().Be(modDefinition);
        sut.AssemblyPath.Should().BeNull();
        sut.IsEnabled.Should().BeFalse();
        sut.IsValid.Should().BeFalse();
        sut.IsLoaded.Should().BeFalse();
        sut.Plugins.Should().BeNull();
        sut.PluginNames.Should().BeNull();
    }

    [Fact]
    public void IsEnabledChangePropagateToPlugins() {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = Substitute.For<IModDefinition>();
        var sut           = new Mod(logger, modDefinition);
        var plugin        = Substitute.For<IPlugin>();

        sut.Plugins = [plugin];

        // Act
        sut.IsEnabled = true;
        sut.IsEnabled = true;
        sut.IsEnabled = false;
        sut.IsEnabled = false;
        sut.IsEnabled = true;

        // Assert
        plugin.Received(2).IsEnabled = true;
        plugin.Received(1).IsEnabled = false;
        sut.PluginNames.Should().BeEquivalentTo(plugin.GetType().FullName!);
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("Scope")]
    public void CreateLogger(string? scope) {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = Substitute.For<IModDefinition>();
        modDefinition.Identifier.Returns("Identifier");
        var sut = new Mod(logger, modDefinition);

        // Act
        var modLogger = sut.CreateLogger(scope);

        // Assert
        modLogger.Should().NotBeNull();
        logger.Received().ForContext("SourceContext", scope == null ? "Identifier" : $"Identifier.{scope}");
    }
}
