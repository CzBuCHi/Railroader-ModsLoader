using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Railroader.ModInjector.JsonConverters;
using Railroader.ModInterfaces;

namespace Railroader_ModInterfaces.Tests.JsonConverters;

public class ModReferenceJsonConverterTests
{
    private sealed class TestData
    {
        [JsonProperty("data")]
        [JsonConverter(typeof(ModReferenceJsonConverter))]
        public Dictionary<string, FluentVersion?> Data { get; set; } = null!;
    }

    [Theory]
    [InlineData("""{"data":{"foo":"1.0.0"}}""", "foo", VersionOperator.GreaterOrEqual, "1.0.0")]
    [InlineData("""{"data":{"foo":""}}""", "foo", null!, null!)]
    [InlineData("""{"data":{"foo":"=1.0.1"}}""", "foo", VersionOperator.Equal, "1.0.1")]
    [InlineData("""{"data":{"foo":"= 1.0.1"}}""", "foo", VersionOperator.Equal, "1.0.1")]
    [InlineData("""{"data":{"foo":">1.0.0"}}""", "foo", VersionOperator.GreaterThan, "1.0.0")]
    [InlineData("""{"data":{"foo":"> 1.0.0"}}""", "foo", VersionOperator.GreaterThan, "1.0.0")]
    [InlineData("""{"data":{"foo":"<=2.0.0"}}""", "foo", VersionOperator.LessOrEqual, "2.0.0")]
    [InlineData("""{"data":{"foo":"<= 2.0.0"}}""", "foo", VersionOperator.LessOrEqual, "2.0.0")]
    [InlineData("""{"data":{"foo":"<1.2.3"}}""", "foo", VersionOperator.LessThan, "1.2.3")]
    [InlineData("""{"data":{"foo":"< 1.2.3"}}""", "foo", VersionOperator.LessThan, "1.2.3")]
    [InlineData("""{"data":{"foo":">=1.0.0"}}""", "foo", VersionOperator.GreaterOrEqual, "1.0.0")]
    [InlineData("""{"data":{"foo":">= 1.0.0"}}""", "foo", VersionOperator.GreaterOrEqual, "1.0.0")]
    public void ReadValidJson(string json, params object[] expected) {
        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.Should().NotBeNull();
        actual.Data.Should().NotBeNull();
        actual.Data.Count.Should().Be(expected.Length / 3, "Incorrect number of dictionary entries");

        for (var i = 0; i < expected.Length; i += 3) {
            var identifier = (string)expected[i];
            var op         = expected[i + 1] as VersionOperator?;
            var version    = expected[i + 2] is string versionString ? Version.Parse(versionString) : null;

            actual.Data.Should().ContainKey(identifier);
            var fluentVersion = actual.Data[identifier];
            if (op == null) {
                fluentVersion.Should().BeNull();
            } else {
                fluentVersion.Should().NotBeNull();
                fluentVersion.Operator.Should().Be(op);
                fluentVersion.Version.Should().Be(version!);
            }
        }
    }

    [Fact]
    public void ReadInvalidVersionFormat() {
        // Arrange
        var json = """{"data":{"foo":"invalid"}}""";

        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage("Invalid version format 'invalid' for mod 'foo'. Expected a valid System.Version (e.g., '1.0.0').");
    }

    [Fact]
    public void ReadInvalidOperator() {
        // Arrange
        var json = """{"data":{"foo":"~1.0.1"}}""";

        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage("Invalid operator '~' for mod 'foo'. Supported operators: =, >, >=, <=, <.");
    }

    [Fact]
    public void ReadInvalidType() {
        // Arrange
        var json = """{"data":{"foo":123}}""";

        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage("Invalid version constraint for mod 'foo' in *Dictionary`2*. Expected a string.");
    }

    [Fact]
    public void ReadNullDictionary() {
        // Arrange
        var json = """{"data":null}""";

        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.Should().NotBeNull();
        actual.Data.Should().NotBeNull();
        actual.Data.Should().BeEmpty();
    }

    [Theory]
    [InlineData(new object[] { "foo", VersionOperator.GreaterOrEqual, "1.0.0" }, """{"data":{"foo":"1.0.0"}}""")]
    [InlineData(new object[] { "foo", null!, null! }, """{"data":{"foo":""}}""")]
    [InlineData(new object[] { "foo", VersionOperator.Equal, "1.0.1" }, """{"data":{"foo":"=1.0.1"}}""")]
    [InlineData(new object[] { "foo", VersionOperator.GreaterThan, "1.0.0" }, """{"data":{"foo":">1.0.0"}}""")]
    [InlineData(new object[] { "foo", VersionOperator.GreaterOrEqual, "1.0.0" }, """{"data":{"foo":">=1.0.0"}}""")]
    [InlineData(new object[] { "foo", VersionOperator.LessOrEqual, "2.0.0" }, """{"data":{"foo":"<=2.0.0"}}""")]
    [InlineData(new object[] { "foo", VersionOperator.LessThan, "1.2.3" }, """{"data":{"foo":"<1.2.3"}}""")]
    public void WriteJson(object[] input, string expected) {
        // Arrange
        var dictionary = new Dictionary<string, FluentVersion?>();
        for (var i = 0; i < input.Length; i += 3) {
            var identifier = (string)input[i];
            var op         = input[i + 1] as VersionOperator?;
            var version    = input[i + 2] is string versionString ? Version.Parse(versionString) : null;
            dictionary.Add(identifier, op == null ? null : new FluentVersion(version!, op.Value));
        }

        var testData = new TestData { Data = dictionary };

        // Act
        var actual = JsonConvert.SerializeObject(testData, Formatting.None);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void WriteJsonEmptyDictionary() {
        // Arrange
        var testData = new TestData { Data = new Dictionary<string, FluentVersion?>() };
        var expected = """{"data":{}}""";

        // Act
        var actual = JsonConvert.SerializeObject(testData, Formatting.None);

        // Assert
        actual.Should().Be(expected);
    }
}
