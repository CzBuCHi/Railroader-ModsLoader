using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Railroader.ModManager.Delegates.Mono.Cecil;
using Railroader.ModManager.Delegates.System.IO.Directory;
using Railroader.ModManager.Delegates.System.IO.File;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Features.CodePatchers;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Features;

public delegate bool ApplyPatchesDelegate(ModDefinition definition, params TypePatcherInfo[] pluginPatchers);

public static class CodePatcher
{
    [ExcludeFromCodeCoverage]
    public static ApplyPatchesDelegate Factory() =>
        (definition, pluginPatchers) => ApplyPatches(Log.Logger.ForSourceContext(),
            AssemblyDefinitionWrapper.ReadAssembly, AssemblyDefinitionWrapper.Write, Directory.GetCurrentDirectory,
            Directory.EnumerateDirectories, File.Delete, File.Move, definition, pluginPatchers);

    public static readonly TypePatcherInfo[] DefaultPluginPatchers = [
        new(typeof(ITopRightButtonPlugin), TopRightButtonPluginPatcher.Factory),
        new(typeof(IHarmonyPlugin), HarmonyPluginPatcher.Factory)
    ];

    private sealed record PatcherContext(
        ILogger Logger,
        ReadAssemblyDefinition ReadAssembly,
        WriteAssemblyDefinition WriteAssembly,
        GetCurrentDirectory GetCurrentDirectory,
        EnumerateDirectories EnumerateDirectories,
        Delete DeleteFile,
        Move MoveFile,
        string AssemblyPath,
        string ModId,
        TypePatcherInfo[] PluginPatchers
    )
    {
        public string TempPath => Path.ChangeExtension(AssemblyPath, ".patched.dll");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool ApplyPatches(
        ILogger logger,
        ReadAssemblyDefinition readAssemblyDefinition,
        WriteAssemblyDefinition writeAssemblyDefinition,
        GetCurrentDirectory getCurrentDirectory,
        EnumerateDirectories enumerateDirectories,
        Delete delete,
        Move move,
        ModDefinition definition,
        TypePatcherInfo[] pluginPatchers
    ) {
        if (pluginPatchers.Length == 0) {
            return true;
        }

        logger.Information("Patching mod {ModId} ...", definition.Identifier);

        var assemblyPath = Path.Combine(definition.BasePath, $"{definition.Identifier}.dll");

        var context = new PatcherContext(logger, readAssemblyDefinition, writeAssemblyDefinition, getCurrentDirectory,
            enumerateDirectories, delete, move, assemblyPath, definition.Identifier, pluginPatchers);

        var patched = context.ApplyPatchesToAssembly();

        if (patched) {
            logger.Information("Patching complete for mod {ModId}", definition.Identifier);
            return true;
        }

        logger.Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath,
            definition.Identifier);
        return false;
    }

    private static bool ApplyPatchesToAssembly(this PatcherContext ctx) {
        using var resolver = ctx.CreateAssemblyResolver();
        using var assembly = ctx.LoadAssembly(resolver);
        if (assembly == null) {
            return false;
        }

        var result = ctx.ApplyPatchersToTypes(assembly);
        if (!result.AnyPatched) {
            return true;
        }

        if (result.HasError) {
            return false;
        }

        ctx.WritePatchedAssembly(assembly);
        ctx.ReplaceOriginalAssembly();
        return true;
    }

    private static DefaultAssemblyResolver CreateAssemblyResolver(this PatcherContext ctx) {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.Combine(ctx.GetCurrentDirectory(), "Railroader_Data", "Managed"));

        var thisModDir = Path.GetDirectoryName(ctx.AssemblyPath)!;
        var modsRoot   = Path.Combine(ctx.GetCurrentDirectory(), "Mods");
        foreach (var modDir in ctx.EnumerateDirectories(modsRoot).Where(d => d != thisModDir)) {
            resolver.AddSearchDirectory(modDir);
        }

        return resolver;
    }

    private static AssemblyDefinition? LoadAssembly(this PatcherContext ctx, DefaultAssemblyResolver resolver) {
        var parameters = new ReaderParameters { AssemblyResolver = resolver };
        var assembly   = ctx.ReadAssembly(ctx.AssemblyPath, parameters);

        if (assembly == null) {
            ctx.Logger.Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", ctx.AssemblyPath,
                ctx.ModId);
        }

        return assembly;
    }

    private sealed record PatchResult(bool AnyPatched, bool HasError);

    private static PatchResult ApplyPatchersToTypes(this PatcherContext ctx, AssemblyDefinition assembly) {
        var anyPatched = false;
        var hasError   = false;

        foreach (var type in assembly.MainModule.Types) {
            var interfaces = type.Interfaces.Select(i => i.InterfaceType?.FullName).Where(n => n != null).ToHashSet();

            var patchers = ctx.PluginPatchers.Where(p => interfaces.Contains(p.MarkerType.FullName))
                              .Select(p => p.Factory())
                              .ToList();

            if (!patchers.Any()) {
                continue;
            }

            anyPatched = true;
            foreach (var patcher in patchers) {
                try {
                    patcher!(assembly, type);
                } catch (Exception ex) {
                    ctx.Logger.Error(ex, "Failed to patch type {TypeName} for mod {ModId}", type.FullName, ctx.ModId);
                    hasError = true;
                }
            }
        }

        if (!anyPatched) {
            ctx.Logger.Information("No patches to assembly {AssemblyPath} for mod {ModId} were applied",
                ctx.AssemblyPath, ctx.ModId);
        }

        return new PatchResult(anyPatched, hasError);
    }

    private static void WritePatchedAssembly(this PatcherContext ctx, AssemblyDefinition assembly) {
        ctx.WriteAssembly(assembly, ctx.TempPath);
        ctx.Logger.Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", ctx.TempPath,
            ctx.ModId);
    }

    private static void ReplaceOriginalAssembly(this PatcherContext ctx) {
        ctx.DeleteFile(ctx.AssemblyPath);
        ctx.MoveFile(ctx.TempPath, ctx.AssemblyPath);
    }
}
