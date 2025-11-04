using System;
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
    public static readonly TypePatcherInfo[] DefaultPluginPatchers = [
        new(typeof(ITopRightButtonPlugin), TopRightButtonPluginPatcher.Factory),
        new(typeof(IHarmonyPlugin), HarmonyPluginPatcher.Factory)
    ];

    [ExcludeFromCodeCoverage]
    public static ApplyPatchesDelegate Create() =>
        Create(
            Log.Logger.ForSourceContext(), AssemblyDefinitionWrapper.ReadAssembly, AssemblyDefinitionWrapper.Write,
            Directory.GetCurrentDirectory, Directory.EnumerateDirectories, File.Delete, File.Move
        );

    public static ApplyPatchesDelegate Create(
        ILogger logger, ReadAssemblyDefinition readAssemblyDefinition, WriteAssemblyDefinition writeAssemblyDefinition,
        GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, Delete delete, Move move
    ) =>
        (definition, pluginPatchers) => ApplyPatches(
            logger, readAssemblyDefinition, writeAssemblyDefinition,
            getCurrentDirectory, enumerateDirectories, delete, move,
            definition, pluginPatchers
        );

    private static bool ApplyPatches(ILogger logger, ReadAssemblyDefinition readAssemblyDefinition, WriteAssemblyDefinition writeAssemblyDefinition, GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, Delete delete, Move move, ModDefinition definition, TypePatcherInfo[] pluginPatchers) {
        if (pluginPatchers.Length == 0) {
            return true;
        }

        logger.Information("Patching mod {ModId} ...", definition.Identifier);

        bool                result;
        var                 assemblyPath       = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
        var                 tempFilePath       = Path.ChangeExtension(assemblyPath, ".patched.dll");
        var                 saveOutput            = false;
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
                logger.Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
                result = false;
            } else {
                var hasPatch = false;
                var hasError = false;
                foreach (var type in assemblyDefinition.MainModule.Types) {
                    try {
                        var interfaces = type.Interfaces.Select(i => i.InterfaceType?.FullName).ToList();
                        var patchers = pluginPatchers.Where(pair => interfaces.Contains(pair.MarkerType.FullName))
                                                     .Select(o => o.Factory());

                        foreach (var patcher in patchers) {
                            hasPatch = true;
                            patcher!(assemblyDefinition, type);
                        }
                    } catch (Exception ex) {
                        logger.Error(ex, "Failed to patch type {TypeName} for mod {ModId}", type.FullName, definition.Identifier);
                        hasError = true;
                    }
                }

                saveOutput = hasPatch && !hasError;
                if (saveOutput) {
                    writeAssemblyDefinition(assemblyDefinition, tempFilePath);
                    logger.Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", tempFilePath, definition.Identifier);
                } else {
                    logger.Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", assemblyPath, definition.Identifier);
                }

                result = !hasPatch || !hasError;
            }
        } finally {
            assemblyDefinition?.Dispose();
            if (saveOutput) {
                delete(assemblyPath);
                move(tempFilePath, assemblyPath);
            }
        }

        if (!result) {
            logger.Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
        } else {
            logger.Information("Patching complete for mod {ModId}", definition.Identifier);
        }

        return result;
    }
}