using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Railroader.ModInjector.JsonConverters;

namespace Railroader_ModInterfaces.Tests.JsonConverters;

public class VersionJsonConverterTests
{
    public static IEnumerable<object?[]> ReadValidJsonData() {
        return Enumerate().Select(o => new object?[] { o.json, o.expected });

        IEnumerable<(string json, Version expected)> Enumerate() {
            yield return ("""{ "version": 0 }""", new Version(0, 0));
            yield return ("""{ "version": 1 }""", new Version(1, 0));
            yield return ("""{ "version": 0.0 }""", new Version(0, 0));
            yield return ("""{ "version": 0.1 }""", new Version(0, 1));
            yield return ("""{ "version": 42.37 }""", new Version(42, 37));
            yield return ("""{ "version": "1" }""", new Version(1, 0));
            yield return ("""{ "version": "1.2" }""", new Version(1, 2));
            yield return ("""{ "version": "1.2.3" }""", new Version(1, 2, 3));
            yield return ("""{ "version": "1.2.3.4" }""", new Version(1, 2, 3, 4));
            
        }
    }

    [Theory]
    [MemberData(nameof(ReadValidJsonData))]
    public void ReadValidJson(string json, Version expected) {
        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.Should().NotBeNull();
        actual.Version.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{ "version": "invalid" }""", "invalid")]
    [InlineData("""{ "version": "-1" }""", "-1")]
    [InlineData("""{ "version": -1 }""", "-1")]
    [InlineData("""{ "version": -1.1 }""", "-1,1")]
    public void ReadInvalidValue(string json, string value) {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonReaderException>()
           .WithMessage($"Unexpected token value '{value}' when reading {typeof(Version)}. Expected: #, #.#, '#', '#.#', '#.#.#' or '#.#.#.#' (# represents characters 0-9)");
    }

    [Theory]
    [InlineData("""{ "version": {} }""", JsonToken.StartObject)]
    [InlineData("""{ "version": null }""", JsonToken.Null)]
    public void ReadInvalidType(string json, JsonToken jsonToken) {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonReaderException>()
           .WithMessage($"Unexpected token type {jsonToken} when reading {typeof(Version)}. Expected: {JsonToken.String} or {JsonToken.Integer} or {JsonToken.Float}.");
    }

    [Fact]
    public void ReadJsonWithoutVersion() {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>("{ }");

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage("Required property 'version' not found in JSON. *");
    }

    public static IEnumerable<object?[]> WriteValidJsonData() {
        return Enumerate().Select(o => new object?[] { o.version, o.expected });

        IEnumerable<(Version version, string expected)> Enumerate() {
            yield return (new Version(1, 2), """{"version":"1.2"}""");
            yield return (new Version(1, 2, 3), """{"version":"1.2.3"}""");
            yield return (new Version(1, 2, 3, 4), """{"version":"1.2.3.4"}""");
        }
    }

    [Theory]
    [MemberData(nameof(WriteValidJsonData))]
    public void WriteValidJson(Version version, string expected) {
        // Act
        var actual = JsonConvert.SerializeObject(new TestData { Version = version });

        // Assert
        actual.Should().Be(expected);
    }

    [UsedImplicitly]
    private sealed class TestData
    {
        [JsonProperty("version", Required = Required.Always)]
        [JsonConverter(typeof(VersionJsonConverter))]
        public Version Version { get; set; } = null!;
    }
}
