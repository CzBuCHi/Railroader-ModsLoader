using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector;
using Serilog;
using Serilog.Events;

namespace Railroader_ModInterfaces.Tests;

public sealed class ModLoaderTests
{
    [Fact]
    public void LoadModDefinitions_ModsDirectoryMissing() {
        // Arrange
        var directory = Substitute.For<IDirectory>();
        directory.EnumerateDirectories(Arg.Any<string>()).Returns([]);

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Returns(directory);

        var sut      = new ModLoader(fileSystem);
        var accessor = new ModLoaderAccessor(sut);

        // Act
        var actual = sut.LoadModDefinitions();

        // Assert
        actual.Should().BeEmpty();
        accessor.LogMessages.Should().BeEmpty();
    }

    [Fact]
    public void LoadModDefinitions_DefinitionJsonMissing() {
        // Arrange
        var directory = Substitute.For<IDirectory>();
        directory.EnumerateDirectories(Arg.Any<string>()).Returns(["A"]);

        var file = Substitute.For<IFile>();
        file.Exists(@"A\Definition.json").Returns(false);

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Returns(directory);
        fileSystem.File.Returns(file);

        var sut      = new ModLoader(fileSystem);
        var accessor = new ModLoaderAccessor(sut);

        // Act
        var actual = sut.LoadModDefinitions();

        // Assert
        actual.Should().BeEmpty();
        accessor.LogMessages.Should().HaveCount(1);
        accessor.LogMessages.Should().ContainEquivalentOf((LogEventLevel.Warning, "Not loading directory {directory}: Missing Definition.json.", new[] { "A" }));
    }

    [Fact]
    public void LoadModDefinitions_DefinitionJsonInvalid() {
        // Arrange
        var directory = Substitute.For<IDirectory>();
        directory.EnumerateDirectories(Arg.Any<string>()).Returns(["A"]);

        var file = Substitute.For<IFile>();
        file.Exists(@"A\Definition.json").Returns(true);
        file.ReadAllText(@"A\Definition.json").Returns("INVALID");

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Returns(directory);
        fileSystem.File.Returns(file);

        var sut      = new ModLoader(fileSystem);
        var accessor = new ModLoaderAccessor(sut);

        // Act
        var actual = sut.LoadModDefinitions();

        // Assert
        actual.Should().BeEmpty();
        accessor.LogMessages.Should().HaveCount(2);
        accessor.LogMessages.Should().ContainEquivalentOf((LogEventLevel.Debug, "Load definition from {directory}...", new[] { "A" }));
        accessor.LogMessages.Should().Contain(o => o.Level == LogEventLevel.Error &&
                                                   o.Format == "Failed to parse definition JSON from {directory}', error: {exception}" &&
                                                   o.Args.Length == 2 &&
                                                   o.Args[0] as string == "A" &&
                                                   o.Args[1] as string == "Invalid JSON: Unexpected end when reading JSON. Path '', line 1, position 7.");
    }

    [Fact]
    public void LoadModDefinitions_DefinitionJsonEmpty() {
        // Arrange
        var directory = Substitute.For<IDirectory>();
        directory.EnumerateDirectories(Arg.Any<string>()).Returns(["A"]);

        var file = Substitute.For<IFile>();
        file.Exists(@"A\Definition.json").Returns(true);
        file.ReadAllText(@"A\Definition.json").Returns("{}");

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Returns(directory);
        fileSystem.File.Returns(file);

        var sut      = new ModLoader(fileSystem);
        var accessor = new ModLoaderAccessor(sut);

        // Act
        var actual = sut.LoadModDefinitions();

        // Assert
        actual.Should().BeEmpty();
        accessor.LogMessages.Should().HaveCount(2);
        accessor.LogMessages.Should().ContainEquivalentOf((LogEventLevel.Debug, "Load definition from {directory}...", new[] { "A" }));
        accessor.LogMessages.Should().Contain(o => o.Level == LogEventLevel.Error &&
                                                   o.Format == "Failed to parse definition JSON from {directory}', error: {exception}" &&
                                                   o.Args.Length == 2 &&
                                                   o.Args[0] as string == "A" &&
                                                   o.Args[1] is ArgumentNullException);
    }

    [Fact]
    public void LoadModDefinitions_DefinitionJsonIncomplete() {
        // Arrange
        var directory = Substitute.For<IDirectory>();
        directory.EnumerateDirectories(Arg.Any<string>()).Returns(["A"]);

        var file = Substitute.For<IFile>();
        file.Exists(@"A\Definition.json").Returns(true);
        file.ReadAllText(@"A\Definition.json").Returns("{\"id\":\"id\",\"name\":\"name\"}");

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Returns(directory);
        fileSystem.File.Returns(file);

        var sut      = new ModLoader(fileSystem);
        var accessor = new ModLoaderAccessor(sut);

        // Act
        var actual = sut.LoadModDefinitions();

        // Assert
        actual.Should().HaveCount(1);
        actual.Should().ContainEquivalentOf(new { Id = "id", Name = "name" });
        accessor.LogMessages.Should().HaveCount(1);
        accessor.LogMessages.Should().ContainEquivalentOf((LogEventLevel.Debug, "Load definition from {directory}...", new[] { "A" }));
    }

    [Fact]
    public void LoadModDefinitions_Conflict() {
        // Arrange
        var directory = Substitute.For<IDirectory>();
        directory.EnumerateDirectories(Arg.Any<string>()).Returns(["A", "B"]);

        var file = Substitute.For<IFile>();
        file.Exists(@"A\Definition.json").Returns(true);
        file.ReadAllText(@"A\Definition.json").Returns("{\"id\":\"id\",\"name\":\"name\"}");
        file.Exists(@"B\Definition.json").Returns(true);
        file.ReadAllText(@"B\Definition.json").Returns("{\"id\":\"id\",\"name\":\"name\"}");

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Returns(directory);
        fileSystem.File.Returns(file);

        var sut      = new ModLoader(fileSystem);
        var accessor = new ModLoaderAccessor(sut);

        // Act
        var actual = sut.LoadModDefinitions();

        // Assert
        actual.Should().HaveCount(1);
        actual.Should().ContainEquivalentOf(new { Id = "id", Name = "name" });
        accessor.LogMessages.Should().HaveCount(3);
        accessor.LogMessages.Should().ContainEquivalentOf((LogEventLevel.Debug, "Load definition from {directory}...", new[] { "A" }));
        accessor.LogMessages.Should().ContainEquivalentOf((LogEventLevel.Debug, "Load definition from {directory}...", new[] { "B" }));
        accessor.LogMessages.Should().ContainEquivalentOf((LogEventLevel.Error, "Another mod with the same ID has been found in {directory}'", new[] { "A" }));
    }

    [Fact]
    public void ProcessLogMessages() {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var logger     = Substitute.For<ILogger>();
        var sut        = new ModLoader(fileSystem);
        new ModLoaderAccessor(sut).LogMessages.AddRange([
            (LogEventLevel.Verbose, "Verbose {msg}", ["Verbose"]),
            (LogEventLevel.Debug, "Debug {msg}", ["Debug"]),
            (LogEventLevel.Information, "Information {msg}", ["Information"]),
            (LogEventLevel.Warning, "Warning {msg}", ["Warning"]),
            (LogEventLevel.Error, "Error {msg}", ["Error"]),
            (LogEventLevel.Fatal, "Fatal {msg}", ["Fatal"])
        ]);

        // Act
        sut.ProcessLogMessages(logger);

        // Assert
        logger.ReceivedCalls().Should().HaveCount(6);
        logger.Received().Verbose("Verbose {msg}", ["Verbose"]);
        logger.Received().Debug("Debug {msg}", ["Debug"]);
        logger.Received().Information("Information {msg}", ["Information"]);
        logger.Received().Warning("Warning {msg}", ["Warning"]);
        logger.Received().Error("Error {msg}", ["Error"]);
        logger.Received().Fatal("Fatal {msg}", ["Fatal"]);
    }

    [Fact]
    public void ProcessLogMessages_ThrowInvalidLevel() {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var logger     = Substitute.For<ILogger>();
        var sut        = new ModLoader(fileSystem);
        new ModLoaderAccessor(sut).LogMessages.AddRange([
            ((LogEventLevel)42, "", [])
        ]);

        // Act
        var act = () => sut.ProcessLogMessages(logger);

        // Assert
        act.Should().Throw<Exception>().WithMessage("Invalid log level (42).");
    }

    private sealed class ModLoaderAccessor(ModLoader modLoader)
    {
        private readonly FieldInfo _LogMessages = typeof(ModLoader).GetField("_LogMessages", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public List<(LogEventLevel Level, string Format, object[] Args)> LogMessages => (List<(LogEventLevel Level, string Format, object[] Args)>)_LogMessages.GetValue(modLoader)!;
    }
}
