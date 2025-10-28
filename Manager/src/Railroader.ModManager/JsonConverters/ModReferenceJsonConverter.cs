using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModManager.Interfaces;

namespace Railroader.ModManager.JsonConverters;

/// <summary> JsonConverter for <see cref="Dictionary{String,FluentVersion}"/>. </summary>
public sealed class ModReferenceJsonConverter : JsonConverter<Dictionary<string, FluentVersion?>>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, Dictionary<string, FluentVersion?>? value, JsonSerializer serializer) {
        writer.WriteStartObject();
        foreach (var pair in value!.OrderBy(o => o.Key)) {
            writer.WritePropertyName(pair.Key!);
            if (pair.Value == null) {
                writer.WriteNull();
            } else {
                writer.WriteValue(pair.Value.ToString());
            }
        }
    }

    /// <inheritdoc />
    public override Dictionary<string, FluentVersion?> ReadJson(JsonReader reader, Type objectType, Dictionary<string, FluentVersion?>? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return new Dictionary<string, FluentVersion?>();
        }

        var jObject = JObject.Load(reader);
        var result  = new Dictionary<string, FluentVersion?>();

        foreach (var property in jObject.Properties()) {
            var identifier = property.Name;
            if (property.Value.Type == JTokenType.Null) {
                result.Add(identifier, null);
                continue;
            }

            if (property.Value.Type != JTokenType.String) {
                throw new JsonSerializationException($"Invalid version constraint for mod '{identifier}' in {objectType.Name}. Expected a string.");
            }

            var constraint    = property.Value.ToString();
            var fluentVersion = ParseVersionConstraint(constraint, identifier);
            result.Add(identifier, fluentVersion);
        }

        return result;
    }

    private static FluentVersion? ParseVersionConstraint(string constraint, string identifier) {
        if (string.IsNullOrEmpty(constraint)) {
            return null;
        }


        var (op, skip) = constraint[0] switch {
            '>' => constraint.Length > 1 && constraint[1] == '=' ? (VersionOperator.GreaterOrEqual, 2) : (VersionOperator.GreaterThan, 1),
            '<' => constraint.Length > 1 && constraint[1] == '=' ? (VersionOperator.LessOrEqual, 2) : (VersionOperator.LessThan, 1),
            '=' => (VersionOperator.Equal, 1),
            '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9'
                => (VersionOperator.GreaterOrEqual, 0),
            _ => (VersionOperator.Equal, -1)
        };

        if (skip != -1) {
            var version = VersionJsonConverter.ParseString(constraint.Substring(skip).TrimStart());
            if (version != null) {
                return new FluentVersion(version, op);
            }
        }


        throw new JsonSerializationException($"Invalid version constraint '{constraint}' for mod '{identifier}'. Expected a valid System.Version or an operator (=, >, >=, <, <=) followed by a version.");
    }
}
