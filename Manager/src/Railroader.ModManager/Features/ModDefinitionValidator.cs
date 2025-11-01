using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Features;

public delegate IReadOnlyList<ModDefinition> ModDefinitionValidatorDelegate(IReadOnlyList<ModDefinition> modDefinitions);

public static class ModDefinitionValidator
{
    [ExcludeFromCodeCoverage]
    public static ModDefinitionValidatorDelegate Factory => definitions => Execute(Log.Logger.ForSourceContext(), definitions);

    public static IReadOnlyList<ModDefinition> Execute(ILogger logger, IReadOnlyList<ModDefinition> modDefinitions) {
        List<string> errors = [];
        if (VerifyRequirementsAndConflicts(modDefinitions, errors)) {
            modDefinitions = SortByDependencies(modDefinitions, errors);
        }

        if (errors.Count > 0) {
            logger.Error("Mod preprocessing failed with error(s): {errors}", errors.ToArray());
            return [];
        }

        return modDefinitions;
    }

    private static bool VerifyRequirementsAndConflicts(IReadOnlyList<ModDefinition> modDefinitions, List<string> errors) {
        var modMap = modDefinitions.ToDictionary(
            mod => mod.Identifier,
            mod => mod,
            StringComparer.OrdinalIgnoreCase);

        foreach (var mod in modDefinitions) {
            // Verify Requirements
            if (mod.Requires != null) {
                foreach (var (requiredId, fluentVersion) in mod.Requires) {
                    if (!modMap.TryGetValue(requiredId, out var requiredMod)) {
                        errors.Add($"Mod '{mod.Identifier}' requires mod '{requiredId}', but it is not present.");
                    }

                    if (fluentVersion != null && !IsVersionSatisfied(requiredMod!.Version, fluentVersion)) {
                        errors.Add($"Mod '{mod.Identifier}' requires mod '{requiredId}' with version constraint '{fluentVersion}', but found version '{requiredMod.Version}'.");
                    }
                }
            }

            // Verify Conflicts
            if (mod.ConflictsWith != null) {
                foreach (var (conflictId, fluentVersion) in mod.ConflictsWith) {
                    if (modMap.TryGetValue(conflictId, out var conflictingMod)) {
                        if (fluentVersion == null || IsVersionSatisfied(conflictingMod.Version, fluentVersion)) {
                            errors.Add($"Mod '{mod.Identifier}' conflicts with mod '{conflictId}' (version: '{conflictingMod.Version}'{(fluentVersion != null ? $", constraint: '{fluentVersion}'" : "")}).");
                        }
                    }
                }
            }
        }

        return errors.Count == 0;
    }

    private static IReadOnlyList<ModDefinition> SortByDependencies(IReadOnlyList<ModDefinition> modDefinitions, List<string> errors) {
        var modMap = modDefinitions.ToDictionary(
            mod => mod.Identifier,
            mod => mod,
            StringComparer.OrdinalIgnoreCase);

        var sorted         = new List<ModDefinition>();
        var visited        = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalidMods    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in modDefinitions) {
            if (!visited.Contains(mod.Identifier)) {
                Visit(mod, new Stack<string>());
            }
        }

        return sorted.ToArray();

        bool Visit(ModDefinition mod, Stack<string> path) {
            if (path.Contains(mod.Identifier)) {
                path.Push(mod.Identifier);
                errors.Add($"Cyclic dependency detected: {string.Join(" -> ", path.Reverse())}");
                return false;
            }

            if (!visited.Add(mod.Identifier)) {
                return !invalidMods.Contains(mod.Identifier);
            }

            path.Push(mod.Identifier);

            var isValid = true;
            if (mod.Requires != null) {
                foreach (var requiredId in mod.Requires.Keys) {
                    if (invalidMods.Contains(requiredId)) {
                        errors.Add($"Mod '{mod.Identifier}' cannot resolve mod '{requiredId}' because mod '{requiredId}' is part of a cyclic dependency.");
                    } else if (!Visit(modMap[requiredId]!, path)) {
                        isValid = false;
                    }
                }
            }

            path.Pop();

            if (isValid) {
                sorted.Add(mod);
            } else {
                invalidMods.Add(mod.Identifier);
            }

            return isValid;
        }
    }

    private static bool IsVersionSatisfied(Version actualVersion, FluentVersion constraint) {
        return constraint.Operator switch {
            VersionOperator.Equal          => actualVersion == constraint.Version,
            VersionOperator.GreaterThan    => actualVersion > constraint.Version,
            VersionOperator.GreaterOrEqual => actualVersion >= constraint.Version,
            VersionOperator.LessThan       => actualVersion < constraint.Version,
            VersionOperator.LessOrEqual    => actualVersion <= constraint.Version,
            _                              => throw new InvalidOperationException($"Unknown version operator: {constraint.Operator}")
        };
    }
}
