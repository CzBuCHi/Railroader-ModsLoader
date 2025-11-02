using System;
using Newtonsoft.Json;
using Serilog.Events;
using Shouldly;

namespace Railroader.ModManager.Tests;

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
        sut.ShouldNotBeNull();
        sut.Identifier.ShouldBe("dummy");
        sut.Name.ShouldBe("Dummy name");
        sut.Version.ShouldBe(new Version(1, 2, 3));
        sut.LogLevel.ShouldBe(LogEventLevel.Debug);
        sut.IsValid.ShouldBeTrue();
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
        sut.ShouldNotBeNull();
        sut.IsValid.ShouldBeFalse();
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
        sut.ShouldNotBeNull();
        sut.IsValid.ShouldBeFalse();
    }
}