using System;
using System.Globalization;
using Newtonsoft.Json;
using TelegraphPoles;

namespace Railroader.ModInjector.JsonConverters;

/// <summary> JsonConverter for <see cref="Version"/>. </summary>
internal sealed class VersionJsonConverter : JsonConverter<Version>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, Version? value, JsonSerializer serializer) {
        writer.WriteValue(value!.ToString());
    }

    /// <inheritdoc />
    public override Version? ReadJson(JsonReader reader, Type objectType, Version? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        switch (reader.TokenType) {
            case JsonToken.String: {
                var raw = Convert.ToString(reader.Value!);
                if (raw.IndexOf('.') == -1) { // to support "version": "1" instead of "1.0"
                    raw += ".0";
                }

                if (!Version.TryParse(raw, out var parsed)) {
                    throw new JsonReaderException($"Unexpected token value '{reader.Value}' when reading {typeof(Version)}. Expected: #, #.#, '#', '#.#', '#.#.#' or '#.#.#.#' (# represents characters 0-9)");
                }

                return parsed;
            }

            case JsonToken.Integer: {
                var raw = Convert.ToInt32(reader.Value!);
                if (raw < 0) {
                    throw new JsonReaderException($"Unexpected token value '{reader.Value}' when reading {typeof(Version)}. Expected: #, #.#, '#', '#.#', '#.#.#' or '#.#.#.#' (# represents characters 0-9)");
                }

                return new Version(raw, 0);
            }

            case JsonToken.Float: {
                var raw      = Convert.ToDecimal(reader.Value!);
                if (raw < 0) {
                    throw new JsonReaderException($"Unexpected token value '{reader.Value}' when reading {typeof(Version)}. Expected: #, #.#, '#', '#.#', '#.#.#' or '#.#.#.#' (# represents characters 0-9)");
                }

                var major = (int)Math.Floor(raw);
                var rest  = raw - major;
                var minor = rest > 0 ? int.Parse(rest.ToString(CultureInfo.InvariantCulture).Substring(2)) : 0;
                return new Version(major, minor);
            }

            default:
                throw new JsonReaderException($"Unexpected token type {reader.TokenType} when reading {typeof(Version)}. Expected: {JsonToken.String} or {JsonToken.Integer} or {JsonToken.Float}.");
        }
    }
}
