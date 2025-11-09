using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO;
using Railroader.ModManager.Interfaces;
using Serilog;
using Shouldly;

namespace Railroader.ModManager.Tests;

public sealed class TestsMod
{
    [Fact]
    public void Constructor() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinition = new ModDefinition();
        var file = Substitute.For<IFileStatic>();
        
        // Act
        var sut = new Mod(logger, modDefinition, file);

        // Assert
        sut.ShouldNotBeNull();
        sut.Definition.ShouldBe(modDefinition);
        sut.AssemblyPath.ShouldBeNull();
        sut.IsEnabled.ShouldBeFalse();
        sut.IsValid.ShouldBeFalse();
        sut.IsLoaded.ShouldBeFalse();
        sut.Plugins.ShouldBeNull();
        sut.PluginNames.ShouldBeNull();
    }

    [Fact]
    public void IsEnabledChangePropagateToPlugins() {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = new ModDefinition();
        var file = Substitute.For<IFileStatic>();
        var sut           = new Mod(logger, modDefinition, file);
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
        sut.PluginNames.ShouldBeEquivalentTo(new[] { plugin.GetType().FullName! });
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("Scope")]
    public void CreateLogger(string? scope) {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = new ModDefinition {
            Identifier = "Identifier"
        };
        
        var file = Substitute.For<IFileStatic>();
        var sut           = new Mod(logger, modDefinition, file);

        // Act
        var modLogger = sut.CreateLogger(scope);

        // Assert
        modLogger.ShouldNotBeNull();
        logger.Received().ForContext("SourceContext", scope == null ? "Identifier" : $"Identifier.{scope}");
    }
    
    [Fact]
    public void LoadSettings_WhenFileNotExists() {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = new ModDefinition {
            BasePath = @"C:\Mod\Path",
            Identifier = "Identifier"
        };
        
        var file = Substitute.For<IFileStatic>();
        var sut           = new Mod(logger, modDefinition, file);

        // Act
        var settings = sut.LoadSettings<Settings>("id");

        // Assert
        settings.ShouldBeNull();
    }
    
    [Fact]
    public void LoadSettings_WhenFileInvalid() {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = new ModDefinition {
            BasePath = @"C:\Mod\Path",
            Identifier = "Identifier"
        };
        
        var file = Substitute.For<IFileStatic>();
        file.ReadAllText(@"C:\Mod\Path\id.json").Returns("invalid json");
        var sut           = new Mod(logger, modDefinition, file);

        // Act
        var act = () => sut.LoadSettings<Settings>("id");

        // Assert
        act.ShouldThrow<JsonException>();
    }
    
    [Fact]
    public void LoadSettings_WhenFileValid() {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = new ModDefinition {
            BasePath = @"C:\Mod\Path",
            Identifier = "Identifier"
        };
        
        var file = Substitute.For<IFileStatic>();
        file.Exists(@"C:\Mod\Path\id.json").Returns(true);
        file.ReadAllText(@"C:\Mod\Path\id.json").Returns(""" { "value": 42 }""");
        var sut           = new Mod(logger, modDefinition, file);
        
        // Act
        var actual = sut.LoadSettings<Settings>("id");

        // Assert
        actual.ShouldNotBeNull();
        actual.Value.ShouldBe(42);
    }
    
    [Fact]
    public void SaveSettings() {
        // Arrange
        var logger        = Substitute.For<ILogger>();
        var modDefinition = new ModDefinition {
            BasePath = @"C:\Mod\Path",
            Identifier = "Identifier"
        };
        
        var file = Substitute.For<IFileStatic>();
        var sut           = new Mod(logger, modDefinition, file);

        // Act
        sut.SaveSettings("id", new Settings { Value = 42 });

        // Assert
        file.Received().WriteAllText(@"C:\Mod\Path\id.json", """{"value":42}""");
    }

    private class Settings
    {
        public int Value { get; set; }
    }
}
