using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Features;

public delegate IReadOnlyList<ModDefinition>
    ModDefinitionValidatorDelegate(IReadOnlyList<ModDefinition> modDefinitions);

public static class ModDefinitionValidator
{
    [ExcludeFromCodeCoverage]
    public static ModDefinitionValidatorDelegate Factory =>
        definitions => Execute(Log.Logger.ForSourceContext(), definitions);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IReadOnlyList<ModDefinition> Execute(ILogger logger, IReadOnlyList<ModDefinition> modDefinitions) {
        var map = modDefinitions.ToDictionary(m => m.Identifier, m => m, StringComparer.OrdinalIgnoreCase);

        return new ValidatorContext(modDefinitions, map).VerifyRequirementsAndConflicts()
                                                        .TopologicalSort()
                                                        .ErrorHandler(errors =>
                                                            logger.Error(
                                                                "Mod preprocessing failed with error(s): {errors}",
                                                                errors))
                                                        .Sorted;
    }

    private sealed record ValidatorContext(
        IReadOnlyList<ModDefinition> ModDefinitions,
        IReadOnlyDictionary<string, ModDefinition> ModMap
    )
    {
        public string[]?                    Errors { get; init; }
        public IReadOnlyList<ModDefinition> Sorted { get; init; } = ModDefinitions;
    }

    private static ValidatorContext VerifyRequirementsAndConflicts(this ValidatorContext ctx) {
        var errors = new List<string>();

        foreach (var mod in ctx.ModDefinitions) {
            errors.AddRange(EnumerateRequireErrors(mod, ctx.ModMap));
            errors.AddRange(EnumerateConflictErrors(mod, ctx.ModMap));
        }

        return errors.Count == 0 ? ctx : ctx with { Errors = errors.ToArray() };
    }

    private static ValidatorContext TopologicalSort(this ValidatorContext ctx) {
        if (ctx.Errors != null) {
            return ctx;
        }

        var sorted  = new List<ModDefinition>(ctx.ModDefinitions.Count);
        var visited = new HashSet<string>(ctx.ModDefinitions.Count, StringComparer.OrdinalIgnoreCase);
        var invalid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors  = new List<string>();

        foreach (var mod in ctx.ModDefinitions) {
            if (visited.Contains(mod.Identifier)) {
                continue;
            }

            var path = new Stack<string>(8);
            if (!Visit(mod, path)) {
                invalid.Add(mod.Identifier);
            }
        }

        return errors.Count == 0 ? ctx with { Sorted = sorted } : ctx with { Errors = errors.ToArray() };

        bool Visit(ModDefinition mod, Stack<string> path) {
            if (path.Contains(mod.Identifier)) {
                path.Push(mod.Identifier);
                errors.Add($"Cyclic dependency detected: {string.Join(" -> ", path.Reverse())}");
                return false;
            }

            if (!visited.Add(mod.Identifier)) {
                return !invalid.Contains(mod.Identifier);
            }

            path.Push(mod.Identifier);
            var ok = true;

            if (mod.Requires != null) {
                foreach (var reqId in mod.Requires.Keys) {
                    if (invalid.Contains(reqId)) {
                        errors.Add(
                            $"Mod '{mod.Identifier}' cannot resolve mod '{reqId}' because mod '{reqId}' is part of a cyclic dependency.");
                        ok = false;
                        continue;
                    }

                    if (!ctx.ModMap.TryGetValue(reqId, out var dep)) {
                        continue;
                    }

                    if (!Visit(dep!, path)) {
                        ok = false;
                    }
                }
            }

            path.Pop();

            if (ok) {
                sorted.Add(mod);
            } else {
                invalid.Add(mod.Identifier);
            }

            return ok;
        }
    }

    private static ValidatorContext ErrorHandler(this ValidatorContext ctx, Action<string[]> handler) {
        if (ctx.Errors == null) {
            return ctx;
        }

        handler(ctx.Errors);
        return ctx with { Sorted = [] };
    }

    private static IEnumerable<string> EnumerateRequireErrors(
        ModDefinition mod,
        IReadOnlyDictionary<string, ModDefinition> map
    ) {
        if (mod.Requires == null) {
            yield break;
        }

        foreach (var (reqId, constraint) in mod.Requires) {
            if (!map.TryGetValue(reqId, out var required)) {
                yield return $"Mod '{mod.Identifier}' requires mod '{reqId}', but it is not present.";
                continue;
            }

            if (constraint != null && !IsVersionSatisfied(required!.Version, constraint)) {
                yield return
                    $"Mod '{mod.Identifier}' requires mod '{reqId}' with version constraint '{constraint}', but found version '{required.Version}'.";
            }
        }
    }

    private static IEnumerable<string> EnumerateConflictErrors(
        ModDefinition mod,
        IReadOnlyDictionary<string, ModDefinition> map
    ) {
        if (mod.ConflictsWith == null) {
            yield break;
        }

        foreach (var (conflictId, constraint) in mod.ConflictsWith) {
            if (!map.TryGetValue(conflictId, out var conflict)) {
                continue;
            }

            if (constraint != null && !IsVersionSatisfied(conflict!.Version, constraint)) {
                continue;
            }

            var extra = constraint != null ? $", constraint: '{constraint}'" : "";
            yield return
                $"Mod '{mod.Identifier}' conflicts with mod '{conflictId}' (version: '{conflict!.Version}'{extra}).";
        }
    }

    private static bool IsVersionSatisfied(Version actual, FluentVersion constraint) =>
        constraint.Operator switch {
            VersionOperator.Equal => actual == constraint.Version,
            VersionOperator.GreaterThan => actual > constraint.Version,
            VersionOperator.GreaterOrEqual => actual >= constraint.Version,
            VersionOperator.LessThan => actual < constraint.Version,
            VersionOperator.LessOrEqual => actual <= constraint.Version,
            _ => throw new InvalidOperationException($"Unknown version operator: {constraint.Operator}")
        };
}
