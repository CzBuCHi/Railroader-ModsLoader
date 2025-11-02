using Newtonsoft.Json;
using Railroader.ModManager.JsonConverters;
using Serilog.Events;
using Shouldly;

namespace Railroader.ModManager.Tests.JsonConverters;

public sealed class TestsLogEventLevelJsonConverter
{
    [Theory]
    [InlineData("""{ "logLevel": "Verbose" }""", LogEventLevel.Verbose)]
    [InlineData("""{ "logLevel": "Debug" }""", LogEventLevel.Debug)]
    [InlineData("""{ "logLevel": "Information" }""", LogEventLevel.Information)]
    [InlineData("""{ "logLevel": "Warning" }""", LogEventLevel.Warning)]
    [InlineData("""{ "logLevel": "Error" }""", LogEventLevel.Error)]
    [InlineData("""{ "logLevel": "Fatal" }""", LogEventLevel.Fatal)]
    [InlineData("""{ "logLevel": "FaTaL" }""", LogEventLevel.Fatal)]
    [InlineData("""{ "logLevel": null }""", null!)]
    [InlineData("{ }", null!)]
    public void ReadValidJson(string json, LogEventLevel? expected) {
        // Act
        var actual = JsonConvert.DeserializeObject<TestData>(json);

        // Assert
        actual.ShouldNotBeNull();
        actual.LogLevel.ShouldBe(expected);
    }

    [Fact]
    public void ReadInvalidValue() {
        // Act & Assert
        Should.Throw<JsonReaderException>(() => JsonConvert.DeserializeObject<TestData>("""{ "logLevel": "invalid" }"""))
              .Message.ShouldBe($"Unexpected token value 'invalid' when reading {typeof(LogEventLevel?)}. Expected: 'Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal' or null");
    }

    [Fact]
    public void ReadInvalidType() {
        // Act & Assert
        Should.Throw<JsonReaderException>(() => JsonConvert.DeserializeObject<TestData>("""{ "logLevel": 42 }"""))
              .Message.ShouldBe($"Unexpected token type {JsonToken.Integer} when reading {typeof(LogEventLevel?)}. Expected: {JsonToken.Null} or {JsonToken.String}.");
    }

    [Fact]
    public void WriteJsonNotSupported() {
        // Act
        var actual = JsonConvert.SerializeObject(new TestData { LogLevel = LogEventLevel.Debug });

        // Assert
        actual.ShouldBe("""{"logLevel":"Debug"}""");
    }

    private sealed class TestData
    {
        [JsonProperty("logLevel")]
        [JsonConverter(typeof(LogEventLevelJsonConverter))]
        public LogEventLevel? LogLevel { get; set; }
    }
}
