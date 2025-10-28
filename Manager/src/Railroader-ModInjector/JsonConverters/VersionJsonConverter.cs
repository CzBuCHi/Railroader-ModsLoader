using System;
using Newtonsoft.Json;

namespace Railroader.ModManager.JsonConverters;

/// <summary> JsonConverter for <see cref="Version"/>. </summary>
internal sealed class VersionJsonConverter : JsonConverter<Version>
{
    private const string Expected = "Expected a valid System.Version (e.g., '1', '1.2', '1.2.3' or '1.2.3.4').";

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, Version? value, JsonSerializer serializer) {
        writer.WriteValue(value!.ToString());
    }

    /// <inheritdoc />
    public override Version ReadJson(JsonReader reader, Type objectType, Version? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.String) {
            var value   = Convert.ToString(reader.Value!);

            var version = ParseString(value);
            if (version == null) {
                throw new JsonSerializationException($"Invalid version format '{value}'. {Expected}");
            }

            return version;
        }

        throw new JsonSerializationException($"Invalid version token {reader.TokenType}. {Expected}");
    }

    internal static Version? ParseString(string value) {
        if (value.IndexOf('.') == -1) { // to support "version": "1" instead of "1.0"
            value += ".0";
        }

        return Version.TryParse(value, out var version) ? version! : null;
    }
}
