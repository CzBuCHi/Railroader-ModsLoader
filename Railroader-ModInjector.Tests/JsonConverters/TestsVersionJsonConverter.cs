using System;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Railroader.ModInjector.JsonConverters;

namespace Railroader.ModInjector.Tests.JsonConverters;

public sealed class TestsVersionJsonConverter
{
    private const string Expected = "Expected a valid System.Version (e.g., '1', '1.2', '1.2.3' or '1.2.3.4').";

    [Theory]
    [InlineData("""{ "version": "1" }""", "1.0")]
    [InlineData("""{ "version": "1.2" }""", "1.2")]
    [InlineData("""{ "version": "1.2.3" }""", "1.2.3")]
    [InlineData("""{ "version": "1.2.3.4" }""", "1.2.3.4")]
    public void ReadValidJson(string json, string version) {
        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.Should().NotBeNull();
        actual.Version.ToString().Should().Be(version);
    }

    [Theory]
    [InlineData("""{ "version": "invalid" }""", "invalid")]
    [InlineData("""{ "version": "-1" }""", "-1")]
    public void ReadInvalidValue(string json, string value) {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage($"Invalid version format '{value}'. {Expected}");
    }

    [Theory]
    [InlineData("""{ "version": {} }""", JsonToken.StartObject)]
    [InlineData("""{ "version": null }""", JsonToken.Null)]
    public void ReadInvalidType(string json, JsonToken jsonToken) {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage($"Invalid version token {jsonToken}. {Expected}");
    }

    [Fact]
    public void ReadJsonWithoutVersion() {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>("{ }");

        // Assert
        act.Should().Throw<JsonSerializationException>()
           .WithMessage("Required property 'version' not found in JSON. *");
    }

    [Fact]
    public void WriteJson() {
        // Act
        var actual = JsonConvert.SerializeObject(new TestData { Version = new Version(1,2,3) });

        // Assert
        actual.Should().Be("""{"version":"1.2.3"}""");
    }

    [UsedImplicitly]
    private sealed class TestData
    {
        [JsonProperty("version", Required = Required.Always)]
        [JsonConverter(typeof(VersionJsonConverter))]
        public Version Version { get; set; } = null!;
    }
}
