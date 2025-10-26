using FluentAssertions;
using Newtonsoft.Json;
using Railroader.ModInjector.JsonConverters;
using Serilog.Events;

namespace Railroader.ModInjector.Tests.JsonConverters;

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
        actual.Should().NotBeNull();
        actual.LogLevel.Should().Be(expected);
    }

    [Fact]
    public void ReadInvalidValue() {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>("""{ "logLevel": "invalid" }""");

        // Assert
        act.Should().Throw<JsonReaderException>()
           .WithMessage($"Unexpected token value 'invalid' when reading {typeof(LogEventLevel?)}. Expected: 'Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal' or null");
    }

    [Fact]
    public void ReadInvalidType() {
        // Act
        var act = () => JsonConvert.DeserializeObject<TestData>("""{ "logLevel": 42 }""");

        // Assert
        act.Should().Throw<JsonReaderException>()
           .WithMessage($"Unexpected token type {JsonToken.Integer} when reading {typeof(LogEventLevel?)}. Expected: {JsonToken.Null} or {JsonToken.String}.");
    }

    [Fact]
    public void WriteJsonNotSupported() {
        // Act
        var actual = JsonConvert.SerializeObject(new TestData { LogLevel = LogEventLevel.Debug });

        // Assert
        actual.Should().Be("""{"logLevel":"Debug"}""");
    }

    private sealed class TestData
    {
        [JsonProperty("logLevel")]
        [JsonConverter(typeof(LogEventLevelJsonConverter))]
        public LogEventLevel? LogLevel { get; set; }
    }
}
