using System;
using FluentAssertions;
using Newtonsoft.Json;
using Railroader.ModInjector;
using Serilog.Events;

namespace Railroader_ModInterfaces.Tests;

public sealed class ModDefinitionTests
{
    [Fact]
    public void LoadFromValidJsonCorrectly() {
        // Arrange
        var json = """
                   {
                       "id": "dummy",
                       "name": "Dummy name",
                       "version": "1.2.3",
                       "logLevel": "Debug"
                   }
                   """;

        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.Should().NotBeNull();
        sut.Identifier.Should().Be("dummy");
        sut.Name.Should().Be("Dummy name");
        sut.Version.Should().Be(new Version(1, 2, 3));
        sut.VersionValid.Should().BeTrue();
        sut.LogLevel.Should().Be(LogEventLevel.Debug);
        sut.LogLevelValid.Should().BeTrue();
        sut.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoadFromInvalidJson_MissingId() {
        // Arrange
        var json = """
                   {
                       "name": "Dummy name",
                       "version": "1.2.3",
                       "logLevel": "Debug"
                   }
                   """;

        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.Should().NotBeNull();
        sut.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoadFromInvalidJson_MissingName() {
        // Arrange
        var json = """
                   {
                       "id": "dummy",
                       "version": "1.2.3",
                       "logLevel": "Debug"
                   }
                   """;

        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.Should().NotBeNull();
        sut.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoadFromInvalidJson_InvalidVersion() {
        // Arrange
        var json = """
                   {
                       "id": "dummy",
                       "name": "Dummy name",
                       "version": "INVALID",
                       "logLevel": "Debug"
                   }
                   """;

        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.Should().NotBeNull();
        sut.IsValid.Should().BeFalse();
        sut.Version.Should().Be(new Version(1, 0));
        sut.VersionValid.Should().BeFalse();
    }

    [Fact]
    public void LoadFromInvalidJson_NullVersion() {
        // Arrange
        var json = """
                   {
                       "id": "dummy",
                       "name": "Dummy name",
                       "version": null,
                       "logLevel": "Debug"
                   }
                   """;

        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.Should().NotBeNull();
        sut.IsValid.Should().BeFalse();
        sut.Version.Should().Be(new Version(1, 0));
        sut.VersionValid.Should().BeFalse();
    }

    [Fact]
    public void LoadFromInvalidJson_InvalidLogLevel() {
        // Arrange
        var json = """
                   {
                       "id": "dummy",
                       "name": "Dummy name",
                       "version": "1.2.3",
                       "logLevel": "INVALID"
                   }
                   """;

        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.Should().NotBeNull();
        sut.IsValid.Should().BeFalse();
        sut.LogLevel.Should().Be(LogEventLevel.Information);
        sut.LogLevelValid.Should().BeFalse();
    }

    [Fact]
    public void LoadFromInvalidJson_NullLogLevel() {
        // Arrange
        var json = """
                   {
                       "id": "dummy",
                       "name": "Dummy name",
                       "version": "1.2.3",
                       "logLevel": null
                   }
                   """;

        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.Should().NotBeNull();
        sut.IsValid.Should().BeTrue();
        sut.LogLevel.Should().Be(LogEventLevel.Information);
        sut.LogLevelValid.Should().BeTrue();
    }
}
