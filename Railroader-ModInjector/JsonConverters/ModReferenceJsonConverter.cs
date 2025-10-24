using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModInterfaces;

namespace Railroader.ModInjector.JsonConverters;

public class ModReferenceJsonConverter : JsonConverter<Dictionary<string, FluentVersion?>>
{
    public override Dictionary<string, FluentVersion?> ReadJson(JsonReader reader, Type objectType, Dictionary<string, FluentVersion?>? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return new Dictionary<string, FluentVersion?>();
        }

        var jObject = JObject.Load(reader);
        var result  = new Dictionary<string, FluentVersion?>();

        foreach (var property in jObject.Properties()) {
            var identifier = property.Name;
            var value      = property.Value.Type == JTokenType.String ? property.Value.ToString() : null;

            if (value == null) {
                throw new JsonSerializationException($"Invalid version constraint for mod '{identifier}' in {objectType.Name}. Expected a string.");
            }

            var fluentVersion = ParseVersionConstraint(identifier, value);
            result.Add(identifier, fluentVersion);
        }

        return result;
    }

    private static FluentVersion? ParseVersionConstraint(string identifier, string constraint) {
        if (string.IsNullOrEmpty(constraint)) {
            return null;
        }

        var (op, skip) = constraint[0] switch {
            '>'                                                                => constraint.Length > 1 && constraint[1] == '=' ? (VersionOperator.GreaterOrEqual, 2) : (VersionOperator.GreaterThan, 1),
            '='                                                                => (VersionOperator.Equal, 1),
            '<'                                                                => constraint.Length > 1 && constraint[1] == '=' ? (VersionOperator.LessOrEqual, 2) : (VersionOperator.LessThan, 1),
            '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' => (VersionOperator.GreaterOrEqual, 0),
            _                                                                  => throw new JsonSerializationException($"Invalid operator '{constraint[0]}' for mod '{identifier}'. Supported operators: =, >, >=, <=, <.")
        };

        var versionString = constraint.Substring(skip).TrimStart();

        Version? version = null;
        if (!string.IsNullOrEmpty(versionString)) {
            if (!Version.TryParse(versionString, out var parsedVersion)) {
                throw new JsonSerializationException($"Invalid version format '{versionString}' for mod '{identifier}'. Expected a valid System.Version (e.g., '1.0.0').");
            }

            version = parsedVersion;
        }

        return new FluentVersion(version!, op);
    }

    public override void WriteJson(JsonWriter writer, Dictionary<string, FluentVersion?>? value, JsonSerializer serializer) {
        var jObject = new JObject();
        foreach (var kvp in value!.OrderBy(k => k.Key)) // Order for consistent output
        {
            var    fluentVersion = kvp.Value;
            string constraintString;

            if (fluentVersion == null) {
                constraintString = "";
            } else {
                var operatorString = fluentVersion.Operator switch {
                    VersionOperator.Equal          => "=",
                    VersionOperator.GreaterThan    => ">",
                    VersionOperator.GreaterOrEqual => ">=",
                    VersionOperator.LessOrEqual    => "<=",
                    VersionOperator.LessThan       => "<",
                    _                              => throw new JsonSerializationException($"Unsupported operator '{fluentVersion.Operator}' for mod '{kvp.Key}'.")
                };
                constraintString = fluentVersion.Operator == VersionOperator.GreaterOrEqual
                    ? fluentVersion.Version.ToString()            // Omit operator for >=
                    : $"{operatorString}{fluentVersion.Version}"; // No space for consistency
            }

            jObject.Add(kvp.Key!, constraintString);
        }

        jObject.WriteTo(writer);
    }
}
