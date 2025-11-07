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

    [Theory]
    [InlineData("abc-ABC_012", true)]
    [InlineData("!", false)]
    public void LoadFrom_Identifier(string value, bool isValid)
    {
        // Arrange
        var json = $$"""
            {
                "id": "{{value}}",
                "name": "name",
                "version": "1.2.3"
            }
            """;
    
        // Act
        var sut = JsonConvert.DeserializeObject<ModDefinition>(json);

        // Assert
        sut.ShouldNotBeNull();
        sut.IsValid.ShouldBe(isValid);

        

    

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
