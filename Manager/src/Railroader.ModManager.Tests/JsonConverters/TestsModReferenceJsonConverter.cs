using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.JsonConverters;
using Shouldly;

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
        actual.ShouldNotBeNull();
        actual.Data.ShouldNotBeNull();
        actual.Data.Count.ShouldBe(1);
        actual.Data.ShouldContainKey("foo");
        var fluentVersion = actual.Data["foo"];

        if (@operator == null) {
            fluentVersion.ShouldBeNull();
        } else {
            fluentVersion.ShouldNotBeNull();
            fluentVersion.Operator.ShouldBe(@operator.Value);
            fluentVersion.Version.ShouldBe(new Version(versionString!));
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
        // Act & Assert
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<TestData>(json))
              .Message.ShouldBe($"Invalid version constraint '{value}' for mod 'foo'. Expected a valid System.Version or an operator (=, >, >=, <, <=) followed by a version.");
    }

    [Fact]
    public void ReadInvalidType() {
        // Arrange
        const string json = """{"data":{"foo":123}}""";

        // Act & Assert
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<TestData>(json))
              .Message.ShouldBe($"Invalid version constraint for mod 'foo' in {typeof(Dictionary<string, FluentVersion>)}. Expected a string.");
    }

    [Fact]
    public void ReadNullDictionary() {
        // Arrange
        const string json = """{"data":null}""";

        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.ShouldNotBeNull();
        actual.Data.ShouldNotBeNull();
        actual.Data.ShouldBeEmpty();
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
        actual.ShouldBe("""{"data":{"bar":null,"foo":">=1.2.3"}}""");
    }
}
