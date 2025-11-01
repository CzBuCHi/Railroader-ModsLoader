using System;
using FluentAssertions;
using MemoryFileSystem;
using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Features;
using Railroader.ModManager.Services;
using Serilog.Events;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsModDefinitionLoader
{
    private static ModDefinitionLoaderDelegate Factory(IMemoryLogger logger, MemoryFs fileSystem) =>
        () => ModDefinitionLoader.LoadDefinitions(logger, fileSystem.Directory.GetCurrentDirectory, fileSystem.Directory.EnumerateDirectories, fileSystem.File.Exists, fileSystem.File.ReadAllText);

    [Fact]
    public void ReturnsEmptyArrayWhenNoDefinitionsFound() {
        // Arrange
        var fileSystem = new MemoryFs();
        var logger     = Substitute.For<IMemoryLogger>();
        var sut        = Factory(logger, fileSystem);

        // Act
        var actual = sut();

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public void SkipsModsWithoutDefinition() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current") {
            { @"C:\Current\Mods\DummyMod\File.txt", "Content" }
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fileSystem);

        // Act
        var definitions = sut();

        // Assert
        definitions.Should().BeEmpty();

        logger.Received().Warning("Not loading directory {directory}: Missing Definition.json.", @"C:\Current\Mods\DummyMod");
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void DetectDuplicateMods() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current") {
            { @"C:\Current\Mods\FirstMod\Definition.json", """{ "id": "Identifier", "name": "Dummy mod", "version": "1.2.3" }""" },
            { @"C:\Current\Mods\SecondMod\Definition.json", """{ "id": "Identifier", "name": "Dummy mod", "version": "1.2.3" }""" }
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fileSystem);

        // Act
        var definitions = sut();

        // Assert
        definitions.Should().HaveCount(1);
        definitions.Should().ContainEquivalentOf(new {
            BasePath = @"C:\Current\Mods\FirstMod",
            Identifier = "Identifier",
            Name = "Dummy mod",
            Version = new Version(1, 2, 3)
        });

        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\SecondMod");
        logger.Received().Error("Another mod with the same Identifier has been found in '{directory}'", @"C:\Current\Mods\FirstMod");
    }

    [Fact]
    public void DetectInvalidDefinitionJson() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current") {
            { @"C:\Current\Mods\FirstMod\Definition.json", "Invalid" }
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fileSystem);

        // Act
        var definitions = sut();

        // Assert
        definitions.Should().HaveCount(0);

        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON, json error: {exception}", Arg.Is<JsonException>(o => true));
    }

    [Fact]
    public void DetectAnyErrorWhenLoadingDefinitionJson() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current") {
            { @"C:\Current\Mods\FirstMod\Definition.json", new InvalidOperationException() }
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fileSystem);

        // Act
        var definitions = sut();

        // Assert
        definitions.Should().HaveCount(0);

        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON, generic error: {exception}", Arg.Any<InvalidOperationException>());
    }

    [Fact]
    public void ReturnsCorrectArray() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current") {
            { @"C:\Current\Mods\DummyMod\Definition.json", """{ "id": "DummyMod", "name": "Dummy mod", "version": "1.2.3", "logLevel": "Debug" }""" },
            { @"C:\Current\Mods\SecondMod\Definition.json", """{ "id": "SecondMod", "name": "Second mod", "version": "1.0.0" }""" }
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fileSystem);

        // Act
        var definitions = sut();

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
            Version = new Version(1, 0, 0)
        });

        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\DummyMod");
        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\SecondMod");
    }
}
