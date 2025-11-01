using System;
using System.Collections.Generic;
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
        Factory(
            Log.Logger.ForSourceContext(),
            AssemblyDefinitionWrapper.ReadAssembly,
            AssemblyDefinitionWrapper.Write,
            Directory.GetCurrentDirectory,
            Directory.EnumerateDirectories,
            File.Delete,
            File.Move
        );

    public static ApplyPatchesDelegate Factory(ILogger logger, ReadAssemblyDefinition readAssemblyDefinition, WriteAssemblyDefinition writeAssemblyDefinition, GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, Delete delete, Move move) =>
        (definition, pluginPatchers) => ApplyPatches(logger, readAssemblyDefinition, writeAssemblyDefinition, getCurrentDirectory, enumerateDirectories, delete, move, definition, pluginPatchers);

    public static readonly List<TypePatcherInfo> PluginPatchers = [
        new(typeof(ITopRightButtonPlugin), TopRightButtonPluginPatcher.Factory),
        new(typeof(IHarmonyPlugin), HarmonyPluginPatcher.Factory)
    ];

    private static bool ApplyPatches(ILogger logger, ReadAssemblyDefinition readAssemblyDefinition, WriteAssemblyDefinition writeAssemblyDefinition, GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, Delete delete, Move move, ModDefinition definition, TypePatcherInfo[] pluginPatchers) {
        if (pluginPatchers.Length == 0) {
            return true;
        }

        logger.Information("Patching mod {ModId} ...", definition.Identifier);

        var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
        if (!ApplyPatches(logger, readAssemblyDefinition, writeAssemblyDefinition, getCurrentDirectory, enumerateDirectories, delete, move, assemblyPath, definition.Identifier, pluginPatchers)) {
            logger.Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
            return false;
        }

        logger.Information("Patching complete for mod {ModId}", definition.Identifier);
        return true;
    }

    private static bool ApplyPatches(ILogger logger, ReadAssemblyDefinition readAssemblyDefinition, WriteAssemblyDefinition writeAssemblyDefinition, GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, Delete delete, Move move, string assemblyPath, string modId, TypePatcherInfo[] pluginPatchers) {
        var tempFilePath = Path.ChangeExtension(assemblyPath, ".patched.dll");
        var success      = false;

        AssemblyDefinition? assemblyDefinition = null;
        try {
            var resolver = new DefaultAssemblyResolver();

            // game DLLs
            resolver.AddSearchDirectory(Path.Combine(getCurrentDirectory(), "Railroader_Data", "Managed"));

            // other mods DLLs
            var thisModDir = Path.GetDirectoryName(assemblyPath);
            var modDirs    = enumerateDirectories(Path.Combine(getCurrentDirectory(), "Mods")).Where(o => o != thisModDir);
            foreach (var modDir in modDirs) {
                resolver.AddSearchDirectory(modDir);
            }

            var readParameters = new ReaderParameters { AssemblyResolver = resolver };
            assemblyDefinition = readAssemblyDefinition(assemblyPath, readParameters);
            if (assemblyDefinition == null) {
                logger.Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", assemblyPath, modId);
                return false;
            }

            var hasPatch = false;
            var hasError = false;
            foreach (var type in assemblyDefinition.MainModule.Types) {
                try {
                    var interfaces = type.Interfaces.Select(i => i.InterfaceType?.FullName).ToList();
                    var patchers = pluginPatchers.Where(pair => interfaces.Contains(pair.MarkerType.FullName))
                                                 .Select(o => o.Factory());

                    foreach (var patcher in patchers) {
                        patcher!(assemblyDefinition, type);
                        hasPatch = true;
                    }
                } catch (Exception ex) {
                    logger.Error(ex, "Failed to patch type {TypeName} for mod {ModId}", type.FullName, modId);
                    hasError = true;
                }
            }

            success = hasPatch && !hasError;

            if (success) {
                writeAssemblyDefinition(assemblyDefinition, tempFilePath);
                logger.Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", tempFilePath, modId);
            } else {
                logger.Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", assemblyPath, modId);
            }

            return true;
        } finally {
            assemblyDefinition?.Dispose();
            if (success) {
                delete(assemblyPath);
                move(tempFilePath, assemblyPath);
            }
        }
    }
}
