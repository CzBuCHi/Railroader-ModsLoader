using System;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using Serilog.Events;

namespace Railroader.ModManager.Tests.Services;

public sealed class TestsModDefinitionLoader
{
    [Fact]
    public void ReturnsEmptyArrayWhenNoDefinitionsFound() {
        // Arrange
        var serviceManager = new TestServiceManager();
        var sut            = serviceManager.CreateModDefinitionLoader();

        // Act
        sut.LoadDefinitions();

        // Assert
        sut.ModDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void SkipsModsWithoutDefinition() {
        // Arrange
        var serviceManager = new TestServiceManager(@"C:\Current");
        serviceManager.MemoryFs.Add(@"C:\Current\Mods\DummyMod\File.txt", "Content");


        var sut = serviceManager.CreateModDefinitionLoader();

        // Act
        sut.LoadDefinitions();

        // Assert
        sut.ModDefinitions.Should().BeEmpty();

        serviceManager.MemoryLogger.Received().Warning("Not loading directory {directory}: Missing Definition.json.", @"C:\Current\Mods\DummyMod");
        serviceManager.MemoryLogger.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void DetectDuplicateMods() {
        // Arrange
        var serviceManager = new TestServiceManager(@"C:\Current");
        serviceManager.MemoryFs.Add(@"C:\Current\Mods\FirstMod\Definition.json", """{ "id": "Identifier", "name": "Dummy mod", "version": "1.2.3" }""");
        serviceManager.MemoryFs.Add(@"C:\Current\Mods\SecondMod\Definition.json", """{ "id": "Identifier", "name": "Dummy mod", "version": "1.2.3" }""");

        var sut    = serviceManager.CreateModDefinitionLoader();

        // Act
        sut.LoadDefinitions();

        // Assert
        sut.ModDefinitions.Should().HaveCount(1);
        sut.ModDefinitions.Should().ContainEquivalentOf(new {
            BasePath = @"C:\Current\Mods\FirstMod",
            Identifier = "Identifier",
            Name = "Dummy mod",
            Version = new Version(1, 2, 3)
        });

        serviceManager.MemoryLogger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        serviceManager.MemoryLogger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\SecondMod");
        serviceManager.MemoryLogger.Received().Error("Another mod with the same Identifier has been found in '{directory}'", @"C:\Current\Mods\FirstMod");
    }

    [Fact]
    public void DetectInvalidDefinitionJson() {
        // Arrange
        var serviceManager = new TestServiceManager(@"C:\Current");
        serviceManager.MemoryFs.Add(@"C:\Current\Mods\FirstMod\Definition.json", "Invalid");

        var sut    = serviceManager.CreateModDefinitionLoader();

        // Act
        sut.LoadDefinitions();

        // Assert
        sut.ModDefinitions.Should().HaveCount(0);

        serviceManager.MemoryLogger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        serviceManager.MemoryLogger.Received().Error("Failed to parse definition JSON, json error: {exception}", Arg.Is<JsonException>(o => true));
    }

    [Fact]
    public void DetectAnyErrorWhenLoadingDefinitionJson() {
        // Arrange
        var serviceManager = new TestServiceManager(@"C:\Current");
        serviceManager.MemoryFs.Add(@"C:\Current\Mods\FirstMod\Definition.json", new InvalidOperationException());

        var sut    = serviceManager.CreateModDefinitionLoader();

        // Act
        sut.LoadDefinitions();

        // Assert
        sut.ModDefinitions.Should().HaveCount(0);

        serviceManager.MemoryLogger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\FirstMod");
        serviceManager.MemoryLogger.Received().Error("Failed to parse definition JSON, generic error: {exception}", Arg.Any<InvalidOperationException>());
    }

    [Fact]
    public void ReturnsCorrectArray() {
        // Arrange
        var serviceManager = new TestServiceManager(@"C:\Current");
        serviceManager.MemoryFs.Add(@"C:\Current\Mods\DummyMod\Definition.json", """{ "id": "DummyMod", "name": "Dummy mod", "version": "1.2.3", "logLevel": "Debug" }""");
        serviceManager.MemoryFs.Add(@"C:\Current\Mods\SecondMod\Definition.json", """{ "id": "SecondMod", "name": "Second mod", "version": "1.0.0" }""");

        var sut    = serviceManager.CreateModDefinitionLoader();

        // Act
        sut.LoadDefinitions();

        // Assert
        sut.ModDefinitions.Should().HaveCount(2);
        sut.ModDefinitions.Should().ContainEquivalentOf(new {
            BasePath = @"C:\Current\Mods\DummyMod",
            Identifier = "DummyMod",
            Name = "Dummy mod",
            Version = new Version(1, 2, 3),
            LogLevel = LogEventLevel.Debug
        });
        sut.ModDefinitions.Should().ContainEquivalentOf(new {
            BasePath = @"C:\Current\Mods\SecondMod",
            Identifier = "SecondMod",
            Name = "Second mod",
            Version = new Version(1, 0, 0)
        });

        serviceManager.MemoryLogger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\DummyMod");
        serviceManager.MemoryLogger.Received().Information("Loading definition from {directory} ...", @"C:\Current\Mods\SecondMod");
    }
}
