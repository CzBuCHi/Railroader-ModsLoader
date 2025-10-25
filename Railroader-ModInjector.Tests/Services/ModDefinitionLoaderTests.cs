using System;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.FileSystem;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers.FileSystem;
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
        var memory = new MemoryFileSystem(@"C:\Current") {
            (@"C:\Current\Mods\DummyMod\File.txt", "Content"),
        };

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = memory.FileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().BeEmpty();

        logger.Received().Warning("Not loading directory {directory}: Missing Definition.json.", @"C:\Current\Mods\DummyMod");
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void DetectDuplicateMods() {
        // Arrange
        var memory = new MemoryFileSystem(@"C:\Current") {
            (@"C:\Current\Mods\FirstMod\Definition.json", """{ "id": "Identifier", "name": "Dummy mod", "version": "1.2.3" }"""),
            (@"C:\Current\Mods\SecondMod\Definition.json", """{ "id": "Identifier", "name": "Dummy mod", "version": "1.2.3" }"""),
        };

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = memory.FileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(1);
        definitions.Should().ContainEquivalentOf(new {
            BasePath = @"C:\Current\Mods\FirstMod",
            Identifier = "Identifier",
            Name = "Dummy mod",
            Version = new Version(1, 2, 3)
        });
        
        logger.Received().Information("Loading definition from {directory}...", @"C:\Current\Mods\FirstMod");
        logger.Received().Information("Loading definition from {directory}...", @"C:\Current\Mods\SecondMod");
        logger.Received().Error("Another mod with the same Identifier has been found in '{directory}'", @"C:\Current\Mods\FirstMod");
    }

    [Fact]
    public void DetectInvalidDefinitionJson() {
        // Arrange
        var memory = new MemoryFileSystem(@"C:\Current") {
            (@"C:\Current\Mods\FirstMod\Definition.json", "Invalid")
        };

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = memory.FileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(0);
        
        logger.Received().Information("Loading definition from {directory}...", @"C:\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON, json error: {exception}", Arg.Is<JsonException>(o => true));
    }

    [Fact]
    public void DetectAnyErrorWhenLoadingDefinitionJson() {
        // Arrange
        var memory = new MemoryFileSystem(@"C:\Current") {
            (@"C:\Current\Mods\FirstMod\Definition.json", new InvalidOperationException())
        };

        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = memory.FileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(0);
        
        logger.Received().Information("Loading definition from {directory}...", @"C:\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON, generic error: {exception}", Arg.Any<InvalidOperationException>());
    }

    [Fact]
    public void ReturnsCorrectArray() {
        // Arrange
        var memory = new MemoryFileSystem(@"C:\Current") {
            (@"C:\Current\Mods\DummyMod\Definition.json", """{ "id": "DummyMod", "name": "Dummy mod", "version": "1.2.3", "logLevel": "Debug" }"""),
            (@"C:\Current\Mods\SecondMod\Definition.json", """{ "id": "SecondMod", "name": "Second mod", "version": "1.0.0" }""")
        };
        
        var logger = Substitute.For<ILogger>();
        var sut    = new ModDefinitionLoader{
            FileSystem = memory.FileSystem,
            Logger = logger
        };

        // Act
        var definitions = sut.LoadDefinitions();

        // Assert
        definitions.Should().HaveCount(2);
        definitions.Should().ContainEquivalentOf(new {
            BasePath = @"C:\Current\Mods\DummyMod",
            Identifier = "DummyMod",
            Name = "Dummy mod",
            Version = new Version(1, 2, 3),
            LogLevel = LogEventLevel.Debug
        });
        definitions.Should().ContainEquivalentOf(new {
            BasePath = @"C:\Current\Mods\SecondMod",
            Identifier = "SecondMod",
            Name = "Second mod",
            Version = new Version(1, 0, 0),
        });

        logger.Received().Information("Loading definition from {directory}...", @"C:\Current\Mods\DummyMod");
        logger.Received().Information("Loading definition from {directory}...", @"C:\Current\Mods\SecondMod");
    }
}
