using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Features;

/// <summary>
///     Delegate that validates and topologically sorts a collection of mod definitions.
/// </summary>
/// <param name="modDefinitions">The mod definitions to validate and sort.</param>
/// <returns>
///     A sorted list of valid mods, or an empty array if validation fails or a cycle is detected.
/// </returns>
public delegate IReadOnlyList<ModDefinition> ValidateMods(IReadOnlyList<ModDefinition> modDefinitions);

/// <summary>
///     Validates mod dependencies, conflicts, and sorts them in load order.
/// </summary>
public static class ModDefinitionValidator
{
    /// <summary>
    ///     Creates a <see cref="ValidateMods" /> delegate that uses the global Serilog logger.
    /// </summary>
    /// <returns>
    ///     A delegate that validates and sorts mods using <see cref="Log.Logger" />.
    /// </returns>
    [ExcludeFromCodeCoverage]
    public static ValidateMods Create => definitions => ValidateAndSort(Log.Logger.ForSourceContext(), definitions);

    /// <summary>
    ///     Validates dependencies and conflicts, then returns a topologically sorted list of mods.
    /// </summary>
    /// <param name="logger">The logger for reporting validation errors and cycles.</param>
    /// <param name="modDefinitions">The collection of mod definitions to process.</param>
    /// <returns>
    ///     A sorted <see cref="IReadOnlyList{ModDefinition}" /> if all checks pass;
    ///     otherwise, an empty array.
    /// </returns>
    public static IReadOnlyList<ModDefinition> ValidateAndSort(ILogger logger, IReadOnlyList<ModDefinition> modDefinitions) =>
        ValidateDependencies(logger, modDefinitions)
            ? SortByDependencies(logger, modDefinitions)
            : [];

    /// <summary>
    ///     Validates that all required mods exist and version constraints are satisfied,
    ///     and that no conflicting mods are present.
    /// </summary>
    /// <param name="logger">The logger for error reporting.</param>
    /// <param name="modDefinitions">The mods to validate.</param>
    /// <returns><c>true</c> if all checks pass; otherwise, <c>false</c>.</returns>
    private static bool ValidateDependencies(ILogger logger, IReadOnlyList<ModDefinition> modDefinitions) {
        var modMap = modDefinitions.ToDictionary(mod => mod.Identifier, mod => mod, StringComparer.OrdinalIgnoreCase);

        var hasError = false;
        foreach (var mod in modDefinitions) {
            // Verify Requirements
            foreach (var (requiredId, fluentVersion) in mod.Requires) {
                hasError = CheckRequirement(logger, mod, modMap, requiredId, fluentVersion) || hasError;
            }

            // Verify Conflicts
            foreach (var (conflictId, fluentVersion) in mod.ConflictsWith) {
                hasError = CheckConflict(logger, mod, modMap, conflictId, fluentVersion) || hasError;
            }
        }

        return !hasError;
    }

    /// <summary>
    ///     Checks a single required mod dependency.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="mod">The mod declaring the requirement.</param>
    /// <param name="modMap">Map of all mods by identifier.</param>
    /// <param name="requiredId">The required mod identifier.</param>
    /// <param name="constraint">Optional version constraint.</param>
    /// <returns><c>true</c> if the requirement fails; otherwise, <c>false</c>.</returns>
    private static bool CheckRequirement(ILogger logger, ModDefinition mod, Dictionary<string, ModDefinition> modMap, string requiredId, FluentVersion? constraint) {
        if (!modMap.TryGetValue(requiredId, out var requiredMod)) {
            logger.Error("Mod '{identifier}' requires mod '{requiredId}', but it is not present.", mod.Identifier, requiredId);
            return true;
        }

        if (constraint == null || Satisfies(requiredMod.Version, constraint)) {
            return false;
        }

        logger.Error(
            "Mod '{identifier}' requires mod '{requiredId}' with version constraint '{fluentVersion}', but found version '{version}'.",
            mod.Identifier, requiredId, constraint, requiredMod.Version
        );
        return true;
    }

    /// <summary>
    ///     Checks a single mod conflict.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="mod">The mod declaring the conflict.</param>
    /// <param name="modMap">Map of all mods by identifier.</param>
    /// <param name="conflictId">The conflicting mod identifier.</param>
    /// <param name="constraint">Optional version constraint.</param>
    /// <returns><c>true</c> if a conflict exists; otherwise, <c>false</c>.</returns>
    private static bool CheckConflict(ILogger logger, ModDefinition mod, Dictionary<string, ModDefinition> modMap, string conflictId, FluentVersion? constraint) {
        if (!modMap.TryGetValue(conflictId, out var conflictingMod)) {
            return false;
        }

        if (constraint != null && !Satisfies(conflictingMod.Version, constraint)) {
            return false;
        }

        if (constraint != null) {
            logger.Error(
                "Mod '{identifier}' conflicts with mod '{conflictId}' (version: '{version}', constraint: '{fluentVersion}').",
                mod.Identifier, conflictId, conflictingMod.Version, constraint
            );
        } else {
            logger.Error(
                "Mod '{identifier}' conflicts with mod '{conflictId}' (version: '{version}').",
                mod.Identifier, conflictId, conflictingMod.Version
            );
        }

        return true;
    }

    /// <summary>
    ///     Performs a topological sort of mods based on <c>Requires</c> dependencies.
    ///     Detects and reports cyclic dependencies.
    /// </summary>
    /// <param name="logger">The logger for cycle detection.</param>
    /// <param name="modDefinitions">The mods to sort.</param>
    /// <returns>
    ///     A sorted list if no cycles exist; otherwise, an empty array (load fails entirely).
    /// </returns>
    /// <remarks>
    ///     Uses depth-first search (DFS) with path tracking to detect cycles.
    ///     <para>
    ///         If a cyclic dependency is detected <strong>anywhere</strong> in the graph,
    ///         <strong>no mods are returned</strong> — the entire load fails.
    ///         This ensures no partial or broken load order.
    ///     </para>
    /// </remarks>
    private static IReadOnlyList<ModDefinition> SortByDependencies(ILogger logger, IReadOnlyList<ModDefinition> modDefinitions) {
        var modMap = modDefinitions.ToDictionary(mod => mod.Identifier, mod => mod, StringComparer.OrdinalIgnoreCase);

        var sorted      = new List<ModDefinition>();
        var visited     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalidMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var hasCycle = false;
        foreach (var mod in modDefinitions) {
            if (!visited.Contains(mod.Identifier)) {
                hasCycle = !Visit(mod, new()) || hasCycle;
            }
        }

        return hasCycle ? [] : sorted.ToArray();

        bool Visit(ModDefinition mod, Stack<string> path) {
            if (path.Contains(mod.Identifier)) {
                path.Push(mod.Identifier);
                logger.Error("Cyclic dependency detected: {dependencyLoop}", string.Join(" -> ", path.Reverse()));
                return false;
            }

            if (!visited.Add(mod.Identifier)) {
                return !invalidMods.Contains(mod.Identifier);
            }

            path.Push(mod.Identifier);

            var isValid = true;
            foreach (var requiredId in mod.Requires.Keys) {
                if (invalidMods.Contains(requiredId)) {
                    logger.Error(
                        "Mod '{identifier}' cannot resolve mod '{requiredId}' because mod is part of a cyclic dependency.",
                        mod.Identifier, requiredId
                    );
                } else if (!Visit(modMap[requiredId]!, path)) {
                    isValid = false;
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

    /// <summary>
    ///     Determines whether a version satisfies a fluent version constraint.
    /// </summary>
    /// <param name="actualVersion">The actual mod version.</param>
    /// <param name="constraint">The version constraint to satisfy.</param>
    /// <returns><c>true</c> if the version meets the constraint; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an unknown <see cref="VersionOperator" /> is encountered.
    /// </exception>
    private static bool Satisfies(Version actualVersion, FluentVersion constraint) {
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