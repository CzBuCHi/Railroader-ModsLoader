using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Railroader.ModManagerInstaller;

[ExcludeFromCodeCoverage]
public class VdfException(string message) : Exception(message);

public delegate VdfEntry VdfEntryLoader(string fullPath);

[PublicAPI]
public class VdfEntry : Dictionary<string, object>
{
    public static VdfEntryLoader Load = LoadEntry;

    public static VdfEntry LoadEntry(string fullPath) {
        var lines = AppServices.File.ReadAllLines(fullPath);
        return Parse(lines);
    }

    public static VdfEntry Parse(string[] lines) {
        var i = 0;
        return Parse(lines, 0, ref i);
    }

    private static VdfEntry Parse(string[] lines, int level, ref int i) {
        var entry = new VdfEntry();
        for (; i < lines.Length; i++) {
            var line = lines[i].Trim();
            if (line == "}") {
                break;
            }

            if (IsSimpleValue(line, out var key, out var value)) {
                entry.Add(key, value);
                continue;
            }

            if (IsKey(line, out key)) {
                i++;
                if (lines[i].Trim() != "{") {
                    throw new VdfException("Expected '{' after key");
                }

                i++;
                var nested = Parse(lines, level + 1, ref i);
                entry.Add(key, nested);
                continue;
            }

            throw new VdfException("Unexpected line in vdf file");
        }

        if (i == lines.Length && level > 0) {
            throw new VdfException("Unexpected end of vdf file");
        }

        return entry;
    }

    private static readonly Regex _SimpleValue = new(@"^\s*""(?<key>[^""]+)""\s+""(?<value>[^""]+)""\s*$", RegexOptions.Compiled);

    private static bool IsSimpleValue(string line, [NotNullWhen(true)] out string? key, [NotNullWhen(true)] out string? value) {
        key = null;
        value = null;

        var match = _SimpleValue.Match(line);
        if (match.Success) {
            key = match.Groups["key"]!.Value;
            value = match.Groups["value"]!.Value;
        }

        return match.Success;
    }

    private static readonly Regex _Key = new(@"^\s*""(?<key>[^""]+)""\s*$", RegexOptions.Compiled);

    private static bool IsKey(string line, [NotNullWhen(true)] out string? key) {
        key = null;

        var match = _Key.Match(line);
        if (match.Success) {
            key = match.Groups["key"]!.Value;
        }

        return match.Success;
    }
}
