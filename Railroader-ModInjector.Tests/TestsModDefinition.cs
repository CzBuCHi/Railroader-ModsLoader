using System;
using FluentAssertions;
using Newtonsoft.Json;
using Serilog.Events;

namespace Railroader.ModInjector.Tests;

public sealed class TestsModDefinition
{
    [Fact]
    public void LoadFromValidJsonCorrectly()
    {
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
        sut.LogLevel.Should().Be(LogEventLevel.Debug);
        sut.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoadFromInvalidJson_MissingId()
    {
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
    public void LoadFromInvalidJson_MissingName()
    {
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
}