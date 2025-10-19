using FluentAssertions;
using Mono.Cecil;
using Newtonsoft.Json;
using Railroader.ModInjector.JsonConverters;
using Serilog.Events;

namespace Railroader_ModInterfaces.Tests.JsonConverters;

public class LogEventLevelJsonConverterTests
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

    [Theory]
    [InlineData(LogEventLevel.Verbose, """{"logLevel":"Verbose"}""")]
    [InlineData(LogEventLevel.Debug, """{"logLevel":"Debug"}""")]
    [InlineData(LogEventLevel.Information, """{"logLevel":"Information"}""")]
    [InlineData(LogEventLevel.Warning, """{"logLevel":"Warning"}""")]
    [InlineData(LogEventLevel.Error, """{"logLevel":"Error"}""")]
    [InlineData(LogEventLevel.Fatal, """{"logLevel":"Fatal"}""")]
    [InlineData(null!, """{"logLevel":null}""")]
    public void WriteJson(LogEventLevel? level, string expected) {
        // Act
        var actual = JsonConvert.SerializeObject(new TestData { LogLevel = level });

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void WriteJsonInvalidValue() {
        // Act
        var act = () => JsonConvert.SerializeObject(new TestData { LogLevel =  (LogEventLevel?)(-1) });

        // Assert
        act.Should().Throw<JsonWriterException>().WithMessage($"Unexpected value of {typeof(LogEventLevel?)} enum: {-1}");
    }

    private sealed class TestData
    {
        [JsonProperty("logLevel")]
        [JsonConverter(typeof(LogEventLevelJsonConverter))]
        public LogEventLevel? LogLevel { get; set; }
    }
}
