using System;
using System.Diagnostics;
using MemoryFileSystem;
using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Features;
using Railroader.ModManager.Services;
using Serilog.Events;
using Shouldly;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsModDefinitionLoader
{
    [DebuggerStepThrough]
    private static LoadDefinitionsDelegate Factory(IMemoryLogger logger, MemoryFs fileSystem) =>
        [DebuggerStepThrough]() => ModDefinitionLoader.LoadDefinitions(logger, fileSystem.Directory.GetCurrentDirectory, fileSystem.Directory.Exists, fileSystem.Directory.EnumerateDirectories, fileSystem.File.Exists, fileSystem.File.ReadAllText);

    [Fact]
    public void ReturnsEmptyArrayWhenModSDirectoryNotFound() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current");
        var logger     = Substitute.For<IMemoryLogger>();
        var sut        = Factory(logger, fileSystem);

        // Act
        var actual = sut();

        // Assert
        actual.ShouldBeEmpty();
        logger.Received().Warning("Mods directory not found at {baseDirectory}", @"C:\Current\Mods");
    }
    
    [Fact]
    public void ReturnsEmptyArrayWhenNoDefinitionsFound() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current"){
            @"C:\Current\Mods"
        };
        var logger     = Substitute.For<IMemoryLogger>();
        var sut        = Factory(logger, fileSystem);

        // Act
        var actual = sut();

        // Assert
        actual.ShouldBeEmpty();
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
        definitions.ShouldBeEmpty();

        logger.Received().Warning("Not loading directory {directory}: Missing Definition.json.", @"C:\Current\Mods\DummyMod");
        logger.ShouldReceiveCallCount(1);
    }

    [Fact]
    public void SkipsModsWithInvalidDefinition() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current") {
            { @"C:\Current\Mods\FirstMod\Definition.json", """{  "name": "Dummy mod" }""" },
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fileSystem);

        // Act
        var definitions = sut();

        // Assert
        definitions.ShouldBeEmpty();

        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        logger.Received().Error("Skipping mod at {definitionPath}: Invalid mod definition.", @"C:\Current\Mods\FirstMod\Definition.json");
        logger.DidNotReceive().Error("Failed to parse definition JSON: {exception}", Arg.Any<Exception>());
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
        definitions.Length.ShouldBe(1);
        definitions.ShouldBeEquivalentTo(new[] {
            new ModDefinition {
                BasePath = @"C:\Current\Mods\FirstMod",
                Identifier = "Identifier",
                Name = "Dummy mod",
                Version = new Version(1, 2, 3)
            }
        });

        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\SecondMod");
        logger.Received().Error("Duplicate mod identifier '{identifier}' found in '{newDirectory}'. Already defined in '{existingDirectory}'.", 
                                "Identifier", @"C:\Current\Mods\SecondMod", @"C:\Current\Mods\FirstMod");
        logger.DidNotReceive().Error("Failed to parse definition JSON: {exception}", Arg.Any<Exception>());

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
        definitions.Length.ShouldBe(0);
        
        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON: {exception}", Arg.Any<JsonReaderException>());
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
        definitions.Length.ShouldBe(0);
        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        logger.Received().Error("Failed to parse definition JSON: {exception}", Arg.Any<InvalidOperationException>());
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
        definitions.Length.ShouldBe(2);
        definitions.ShouldBeEquivalentTo(new[] {
            new ModDefinition {
                BasePath = @"C:\Current\Mods\DummyMod",
                Identifier = "DummyMod",
                Name = "Dummy mod",
                Version = new Version(1, 2, 3),
                LogLevel = LogEventLevel.Debug
            },
            new ModDefinition {
                BasePath = @"C:\Current\Mods\SecondMod",
                Identifier = "SecondMod",
                Name = "Second mod",
                Version = new Version(1, 0, 0)
            }
        });

        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\DummyMod");
        logger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\SecondMod");
    }

    [Fact]
    public void SkipsModWithMissingDefinition_AndLogsWarning()
    {
        var fileSystem = new MemoryFs(@"C:\Current") {
            @"C:\Current\Mods\BadMod\"
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fileSystem);

        sut().ShouldBeEmpty();

        logger.Received().Warning("Not loading directory {directory}: Missing Definition.json.", @"C:\Current\Mods\BadMod");
        fileSystem.File.ReadAllText.Received(0).Invoke(Arg.Any<string>());
    }

    [Fact]
    public void DuplicateIdentifier_SkipsSecondAndLogsError()
    {
        var fs = new MemoryFs(@"C:\Current") {
            { @"C:\Current\Mods\First\Definition.json",  "{ \"id\": \"Dup\", \"name\": \"A\", \"version\": \"1.0\" }" },
            { @"C:\Current\Mods\Second\Definition.json", "{ \"id\": \"Dup\", \"name\": \"B\", \"version\": \"1.0\" }" }
        };
        var logger = Substitute.For<IMemoryLogger>();
        var sut    = Factory(logger, fs);

        var result = sut();
        result.Length.ShouldBe(1);
        result[0].BasePath.ShouldBe(@"C:\Current\Mods\First");

        logger.Received().Error("Duplicate mod identifier '{identifier}' found in '{newDirectory}'. Already defined in '{existingDirectory}'.",
                                "Dup", @"C:\Current\Mods\Second", @"C:\Current\Mods\First");
    }
}
