using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Railroader.ModManager.JsonConverters;
using Shouldly;

namespace Railroader.ModManager.Tests.JsonConverters;

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
        actual.ShouldNotBeNull();
        actual.Version.ToString().ShouldBe(version);
    }

    [Theory]
    [InlineData("""{ "version": "invalid" }""", "invalid")]
    [InlineData("""{ "version": "-1" }""", "-1")]
    public void ReadInvalidValue(string json, string value) {
        // Act & Assert
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<TestData>(json))
              .Message.ShouldBe($"Invalid version format '{value}'. {Expected}");
    }

    [Theory]
    [InlineData("""{ "version": {} }""", JsonToken.StartObject)]
    [InlineData("""{ "version": null }""", JsonToken.Null)]
    public void ReadInvalidType(string json, JsonToken jsonToken) {
        // Act & Assert
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<TestData>(json))
              .Message.ShouldBe($"Invalid version token {jsonToken}. {Expected}");
    }

    [Fact]
    public void ReadJsonWithoutVersion() {
        // Act & Assert
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<TestData>("{ }"))
              .Message.ShouldStartWith("Required property 'version' not found in JSON.");
    }

    [Fact]
    public void WriteJson() {
        // Act
        var actual = JsonConvert.SerializeObject(new TestData { Version = new Version(1,2,3) });

        // Assert
        actual.ShouldBe("""{"version":"1.2.3"}""");
    }

    [UsedImplicitly]
    private sealed class TestData
    {
        [JsonProperty("version", Required = Required.Always)]
        [JsonConverter(typeof(VersionJsonConverter))]
        public Version Version { get; set; } = null!;
    }
}
