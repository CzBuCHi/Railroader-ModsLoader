using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.JsonConverters;

namespace Railroader.ModManager.Tests.JsonConverters;

public sealed class TestsModReferenceJsonConverter
{
    private sealed class TestData
    {
        [JsonProperty("data")]
        [JsonConverter(typeof(ModReferenceJsonConverter))]
        public Dictionary<string, FluentVersion?> Data { get; set; } = null!;
    }

    [Theory]
    [InlineData("""{"data":{"foo":"1"}}""", VersionOperator.GreaterOrEqual, "1.0")]
    [InlineData("""{"data":{"foo":"1.0.0"}}""", VersionOperator.GreaterOrEqual, "1.0.0")]
    [InlineData("""{"data":{"foo": null }}""", null!, null!)]
    [InlineData("""{"data":{"foo": "" }}""", null!, null!)]
    [InlineData("""{"data":{"foo":"=1.0.1"}}""", VersionOperator.Equal, "1.0.1")]
    [InlineData("""{"data":{"foo":"= 1.0.1"}}""", VersionOperator.Equal, "1.0.1")]
    [InlineData("""{"data":{"foo":">1.0.0"}}""", VersionOperator.GreaterThan, "1.0.0")]
    [InlineData("""{"data":{"foo":"> 1.0.0"}}""", VersionOperator.GreaterThan, "1.0.0")]
    [InlineData("""{"data":{"foo":"<=2.0.0"}}""", VersionOperator.LessOrEqual, "2.0.0")]
    [InlineData("""{"data":{"foo":"<= 2.0.0"}}""", VersionOperator.LessOrEqual, "2.0.0")]
    [InlineData("""{"data":{"foo":"<1.2.3"}}""", VersionOperator.LessThan, "1.2.3")]
    [InlineData("""{"data":{"foo":"< 1.2.3"}}""", VersionOperator.LessThan, "1.2.3")]
    [InlineData("""{"data":{"foo":">=1.0.0"}}""", VersionOperator.GreaterOrEqual, "1.0.0")]
    [InlineData("""{"data":{"foo":">= 1.0.0"}}""", VersionOperator.GreaterOrEqual, "1.0.0")]
    public void ReadValidJson(string json, VersionOperator? @operator, string? versionString) {
        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.Should().NotBeNull();
        actual.Data.Should().NotBeNull();
        actual.Data.Count.Should().Be(1);
        var fluentVersion = actual.Data.Should().ContainKey("foo").WhoseValue;

        if (@operator == null) {
            fluentVersion.Should().BeNull();
        } else {
            fluentVersion.Should().NotBeNull();
            fluentVersion.Operator.Should().Be(@operator);
            fluentVersion.Version.Should().Be(new Version(versionString!));
        }
    }

    [Theory]
    [InlineData("""{"data":{"foo":"invalid"}}""", "invalid")]
    [InlineData("""{"data":{"foo":"~1.0.1"}}""", "~1.0.1")]
    [InlineData("""{"data":{"foo":">"}}""", ">")]
    [InlineData("""{"data":{"foo":"<"}}""", "<")]
    [InlineData("""{"data":{"foo":">="}}""", ">=")]
    [InlineData("""{"data":{"foo":">=invalid"}}""", ">=invalid")]
    public void ReadInvalidJson(string json, string value) {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage($"Invalid version constraint '{value}' for mod 'foo'. Expected a valid System.Version or an operator (=, >, >=, <, <=) followed by a version.");
    }

    [Fact]
    public void ReadInvalidType() {
        // Arrange
        const string json = """{"data":{"foo":123}}""";

        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage("Invalid version constraint for mod 'foo' in *Dictionary*. Expected a string.");
    }

    [Fact]
    public void ReadNullDictionary() {
        // Arrange
        const string json = """{"data":null}""";

        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.Should().NotBeNull();
        actual.Data.Should().NotBeNull();
        actual.Data.Should().BeEmpty();
    }

    [Fact]
    public void WriteJson() {
        // Arrange
        var testData = new TestData {
            Data = new Dictionary<string, FluentVersion?> {
                { "foo", new FluentVersion(new Version(1, 2, 3), VersionOperator.GreaterOrEqual) },
                { "bar", null }
            }
        };

        // Act
        var actual = JsonConvert.SerializeObject(testData);

        // Assert
        actual.Should().Be("""{"data":{"bar":null,"foo":">=1.2.3"}}""");
    }
}
