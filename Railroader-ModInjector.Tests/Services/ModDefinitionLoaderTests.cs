using System;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using Railroader_ModInterfaces.Tests.Wrappers.FileSystemWrapper;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Serilog;
using Serilog.Events;

namespace Railroader_ModInterfaces.Tests.Services;

public sealed class ModDefinitionLoaderTests
{
    [Fact]
    public void ReturnsEmptyArrayWhenNoDefinitionsFound() {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var logger     = Substitute.For<ILogger>();
        var sut = new ModDefinitionLoader {
            FileSystem = fileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().BeEmpty();
    }

    [Fact]
    public void SkipsModsWithoutDefinition() {
        // Arrange
        var fileSystem = new MockFileSystem {
            new MockFileSystemFile(@"\Current\Mods\DummyMod\File.txt", "Content"),
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = fileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().BeEmpty();
    }

    [Fact]
    public void DetectDuplicateMods() {
        // Arrange
        var fileSystem = new MockFileSystem {
            new MockFileSystemFile(
                @"\Current\Mods\FirstMod\Definition.json",
                """
                {
                    "id": "Identifier",
                    "name": "Dummy mod",
                    "version": "1.2.3",
                    "logLevel": "Debug"
                }
                """),
            new MockFileSystemFile(
                @"\Current\Mods\SecondMod\Definition.json",
                """
                {
                    "id": "Identifier",
                    "name": "Second mod",
                    "version": "1.0.0"
                }
                """)
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = fileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(1);
        definitions.Should().ContainEquivalentOf(new {
            BasePath = @"\Current\Mods\FirstMod",
            Identifier = "Identifier",
            Name = "Dummy mod",
            Version = new Version(1, 2, 3),
            LogLevel = LogEventLevel.Debug
        });
        
        logger.Received().Information("Loading definition from {directory}...", @"\Current\Mods\FirstMod");
        logger.Received().Information("Loading definition from {directory}...", @"\Current\Mods\SecondMod");
        logger.Received().Error("Another mod with the same Identifier has been found in '{directory}'", @"\Current\Mods\FirstMod");
    }

    [Fact]
    public void DetectInvalidDefinitionJson() {
        // Arrange
        var fileSystem = new MockFileSystem {
            new MockFileSystemFile(@"\Current\Mods\FirstMod\Definition.json", "Invalid")
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = fileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(0);
        
        logger.Received().Information("Loading definition from {directory}...", @"\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON, json error: {exception}", Arg.Is<JsonException>(o => true));
    }

    [Fact]
    public void DetectAnyErrorWhenLoadingDefinitionJson() {
        // Arrange
        var fileSystem = new MockFileSystem {
            new MockFileSystemFile(@"\Current\Mods\FirstMod\Definition.json", "Content", new InvalidOperationException())
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = fileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(0);
        
        logger.Received().Information("Loading definition from {directory}...", @"\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON, generic error: {exception}", Arg.Any<InvalidOperationException>());
    }

    [Fact]
    public void ReturnsCorrectArray() {
        // Arrange
        var fileSystem = new MockFileSystem {
            new MockFileSystemFile(
                @"\Current\Mods\DummyMod\Definition.json",
                """
                {
                    "id": "DummyMod",
                    "name": "Dummy mod",
                    "version": "1.2.3",
                    "logLevel": "Debug"
                }
                """),
            new MockFileSystemFile(
                @"\Current\Mods\SecondMod\Definition.json",
                """
                {
                    "id": "SecondMod",
                    "name": "Second mod",
                    "version": "1.0.0"
                }
                """)
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = fileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(2);
        definitions.Should().ContainEquivalentOf(new {
            BasePath = @"\Current\Mods\DummyMod",
            Identifier = "DummyMod",
            Name = "Dummy mod",
            Version = new Version(1, 2, 3),
            LogLevel = LogEventLevel.Debug
        });
        definitions.Should().ContainEquivalentOf(new {
            BasePath = @"\Current\Mods\SecondMod",
            Identifier = "SecondMod",
            Name = "Second mod",
            Version = new Version(1, 0, 0),
        });

        logger.Received().Information("Loading definition from {directory}...", @"\Current\Mods\DummyMod");
        logger.Received().Information("Loading definition from {directory}...", @"\Current\Mods\SecondMod");
    }
}
