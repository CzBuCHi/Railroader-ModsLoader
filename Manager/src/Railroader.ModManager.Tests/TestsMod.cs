using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Interfaces;

namespace Railroader.ModManager.Tests;

public sealed class TestsMod
{
    [Fact]
    public void Constructor() {
        // Arrange
        const string assemblyPath = "assemblyPath";

        var modDefinition = Substitute.For<IModDefinition>();

        // Act
        var sut = new Mod(modDefinition, assemblyPath);

        // Assert
        sut.Should().NotBeNull();
        sut.Definition.Should().Be(modDefinition);
        sut.AssemblyPath.Should().Be(assemblyPath);
        sut.IsEnabled.Should().BeFalse();
        sut.IsLoaded.Should().BeFalse();
        sut.Plugins.Should().BeNull();
        sut.PluginNames.Should().BeNull();
    }

    [Fact]
    public void IsEnabledChangePropagateToPlugins() {
        // Arrange
        var modDefinition = Substitute.For<IModDefinition>();
        var sut           = new Mod(modDefinition, "assemblyPath");
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
    [InlineData("scope")]
    public void CreateLogger(string? scope) {
        // Arrange
        var serviceManager = new TestServiceManager();
        
        var modDefinition = Substitute.For<IModDefinition>();
        modDefinition.Identifier.Returns("Identifier");
        var sut = new Mod(modDefinition, "assemblyPath");
      
        // Act
        var actual = sut.CreateLogger(scope);

        // Assert
        actual.Should().Be(serviceManager.ContextLogger);

        serviceManager.LoggerFactory.Received().GetLogger(scope == null ? "Identifier" : $"Identifier.{scope}");
    }
}
