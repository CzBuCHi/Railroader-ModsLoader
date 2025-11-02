using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModManager.Delegates.System.IO.Directory;
using Railroader.ModManager.Delegates.System.IO.File;
using Railroader.ModManager.Services;
using Path = System.IO.Path;

namespace Railroader.ModManager.Features;

public delegate ModDefinition[] ModDefinitionLoaderDelegate();

public static class ModDefinitionLoader
{
    private sealed record LoaderContext(
        IMemoryLogger Logger,
        EnumerateDirectories EnumerateDirectories,
        Exists Exists,
        ReadAllText ReadAllText,
        string BaseDirectory
    )
    {
        public List<ModDefinition>               Mods   { get; } = [];
        public Dictionary<string, ModDefinition> ModMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    [ExcludeFromCodeCoverage]
    public static ModDefinitionLoaderDelegate Factory(IMemoryLogger logger) =>
        () => LoadDefinitions(logger, Directory.GetCurrentDirectory, Directory.EnumerateDirectories, File.Exists,
            File.ReadAllText);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ModDefinition[] LoadDefinitions(
        IMemoryLogger logger,
        GetCurrentDirectory getCurrentDirectory,
        EnumerateDirectories enumerateDirectories,
        Exists exists,
        ReadAllText readAllText
    ) =>
        new LoaderContext(logger, enumerateDirectories, exists, readAllText,
                Path.Combine(getCurrentDirectory(), "Mods")).ScanModDirectories()
                                                            .Mods.ToArray();

    private static LoaderContext ScanModDirectories(this LoaderContext ctx) {
        foreach (var dir in ctx.EnumerateDirectories(ctx.BaseDirectory)) {
            var defPath = Path.Combine(dir, "Definition.json");
            if (!ctx.Exists(defPath)) {
                ctx.Logger.Warning("Not loading directory {directory}: Missing Definition.json.", dir);
                continue;
            }

            ctx.Logger.Information("Loading definition from {directory} ...", dir);
            ctx = ctx.TryLoadMod(dir, defPath);
        }

        return ctx;
    }

    private static LoaderContext TryLoadMod(this LoaderContext ctx, string dir, string defPath) {
        try {
            var mod = JObject.Parse(ctx.ReadAllText(defPath)).ToObject<ModDefinition>()!;
            if (ctx.ModMap.TryGetValue(mod.Identifier, out var existing)) {
                ctx.Logger.Error("Another mod with the same Identifier has been found in '{directory}'",
                    existing.BasePath);
                return ctx;
            }

            mod.BasePath = dir;
            ctx.ModMap[mod.Identifier] = mod;
            ctx.Mods.Add(mod);
        } catch (JsonException ex) {
            ctx.Logger.Error("Failed to parse definition JSON, json error: {exception}", ex);
        } catch (Exception ex) {
            ctx.Logger.Error("Failed to parse definition JSON, generic error: {exception}", ex);
        }

        return ctx;
    }
}
