using System;
using System.Linq;
using Newtonsoft.Json;
using Serilog.Events;

namespace Railroader.ModManager.JsonConverters;

/// <summary> JsonConverter for <see cref="LogEventLevel"/>. </summary>
internal sealed class LogEventLevelJsonConverter : JsonConverter<LogEventLevel?>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, LogEventLevel? value, JsonSerializer serializer) {
        writer.WriteValue(value.ToString());
    }

    /// <inheritdoc />
    public override LogEventLevel? ReadJson(JsonReader reader, Type objectType, LogEventLevel? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        switch (reader.TokenType) {
            case JsonToken.Null:
                return null;

            case JsonToken.String: {
                var raw = (string)reader.Value!;
                if (!Enum.TryParse<LogEventLevel>(raw, true, out var parsed)) {
                    var validValues = Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>().Select(o => $"'{o}'");
                    throw new JsonReaderException($"Unexpected token value '{raw}' when reading {typeof(LogEventLevel?)}. Expected: {string.Join(", ", validValues)} or null");
                }

                return parsed;
            }

            default:
                throw new JsonReaderException($"Unexpected token type {reader.TokenType} when reading {typeof(LogEventLevel?)}. Expected: {JsonToken.Null} or {JsonToken.String}.");
        }
    }
}
