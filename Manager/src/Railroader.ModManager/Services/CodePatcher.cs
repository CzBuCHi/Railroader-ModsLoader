using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Railroader.ModManager.CodePatchers;
using Railroader.ModManager.CodePatchers.Special;
using Railroader.ModManager.Delegates;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Services.Wrappers.FileSystem;
using Serilog;

namespace Railroader.ModManager.Services;

internal interface ICodePatcher
{
    /// <summary> Gets or sets the mapping of plugin marker interfaces to their corresponding patcher types. </summary>
    /// <remarks>Plugins are applied in reverse order.</remarks>
    List<(Type InterfaceType, Type PluginPatcherType)> PluginPatchers { get; }

    void ApplyPatches(ModDefinition definition);
}

/// <summary> Compiles mod source code into a DLL and applies registered patches to the assembly. </summary>
internal sealed class CodePatcher(IFileSystem fileSystem, ReadAssemblyDefinitionDelegate readAssemblyDefinition, WriteAssemblyDefinitionDelegate writeAssemblyDefinition, ILogger logger)
    : ICodePatcher
{
    private readonly ConcurrentDictionary<Type, ITypePatcher> _PluginPatchers = new();

    /// <inheritdoc />
    public List<(Type InterfaceType, Type PluginPatcherType)> PluginPatchers { get; set; } = [
        (typeof(ITopRightButtonPlugin), typeof(TopRightButtonPluginPatcher)),
        (typeof(IHarmonyPlugin), typeof(HarmonyPluginPatcher))
    ];

    /// <inheritdoc />
    public void ApplyPatches(ModDefinition definition) {
        if (PluginPatchers.Count > 0) {
            logger.Information("Patching mod {ModId} ...", definition.Identifier);

            var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
            if (!ApplyPatches(assemblyPath, definition.Identifier)) {
                logger.Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
                return;
            }

            logger.Information("Patching complete for mod {ModId}", definition.Identifier);
        }
    }

    /// <summary> Applies registered patchers to the compiled assembly. </summary>
    /// <param name="assemblyPath">The path to the assembly to patch. Must not be null.</param>
    /// <param name="modId">The identifier of the mod, used for logging.</param>
    /// <returns>True if patching succeeds, false otherwise.</returns>
    private bool ApplyPatches(string assemblyPath, string modId) {
        var tempFilePath = Path.ChangeExtension(assemblyPath, ".patched.dll");
        var success      = false;

        AssemblyDefinition? assemblyDefinition = null;
        try {
            var resolver = new DefaultAssemblyResolver();

            // game DLLs
            resolver.AddSearchDirectory(Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed"));

            // other mods DLLs
            var thisModDir = Path.GetDirectoryName(assemblyPath);
            var modDirs = fileSystem.Directory.EnumerateDirectories(Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Mods"))
                                    .Where(o => o != thisModDir);
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
                    var patchers = PluginPatchers.Where(pair => interfaces.Contains(pair.InterfaceType!.FullName))
                                                 .Select(pair => _PluginPatchers.GetOrAdd(pair.InterfaceType,
                                                     _ => (ITypePatcher)Activator.CreateInstance(pair.PluginPatcherType!, logger)!
                                                 ));
                    foreach (var patcher in patchers) {
                        patcher!.Patch(assemblyDefinition, type);
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
                fileSystem.File.Delete(assemblyPath);
                fileSystem.File.Move(tempFilePath, assemblyPath);
            }
        }
    }
}
