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

/// <summary>
///     Action that applies post-compilation patches to a mod assembly using Mono.Cecil.
/// </summary>
/// <param name="definition">The mod definition to patch.</param>
/// <param name="pluginPatchers">
///     Optional patchers. If <c>null</c>, <see cref="CodePatcher.DefaultPluginPatchers" /> are used.
/// </param>
/// <returns><c>true</c> if patching succeeded or was not needed; <c>false</c> on error.</returns>
public delegate bool PatchModAction(ModDefinition definition, TypePatcherInfo[]? pluginPatchers = null);

/// <summary>
///     Applies runtime patches to a compiled mod assembly (UI buttons, Harmony hooks, etc.).
///     All I/O and assembly operations are injected via delegates for full unit-test isolation.
/// </summary>
public static class CodePatcher
{
    /// <summary>
    ///     Default built-in patchers for common plugin interfaces.
    /// </summary>
    public static readonly TypePatcherInfo[] DefaultPluginPatchers = [
        new(typeof(ITopRightButtonPlugin), TopRightButtonPluginPatcher.Factory),
        new(typeof(IHarmonyPlugin), HarmonyPluginPatcher.Factory)
    ];

    /// <summary>
    ///     Creates a <see cref="PatchModAction" /> pre-configured with production dependencies.
    /// </summary>
    /// <returns>A fully-wired delegate ready for use.</returns>
    [ExcludeFromCodeCoverage]
    public static PatchModAction Create() =>
        (definition, pluginPatchers) => ApplyPatches(
            Log.Logger.ForSourceContext(), AssemblyDefinitionWrapper.ReadAssembly, AssemblyDefinitionWrapper.Write,
            Directory.GetCurrentDirectory, Directory.EnumerateDirectories, File.Delete, File.Move,
            definition, pluginPatchers ?? DefaultPluginPatchers
        );

    /// <summary>
    ///     Executes all registered <paramref name="pluginPatchers" /> against the compiled mod DLL.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="readAssemblyDefinition">Delegate to read an assembly into an <see cref="AssemblyDefinition" />.</param>
    /// <param name="writeAssemblyDefinition">Delegate to write an <see cref="AssemblyDefinition" /> to disk.</param>
    /// <param name="getCurrentDirectory">Delegate returning the process current directory.</param>
    /// <param name="enumerateDirectories">Delegate to enumerate subdirectories.</param>
    /// <param name="delete">Delegate to delete a file.</param>
    /// <param name="move">Delegate to move/rename a file.</param>
    /// <param name="definition">Mod definition containing path and identifier.</param>
    /// <param name="pluginPatchers">Array of patchers to apply.</param>
    /// <returns><c>true</c> if patching succeeded or no patches were needed; <c>false</c> on any error.</returns>
    public static bool ApplyPatches(
        ILogger logger, ReadAssemblyDefinition readAssemblyDefinition, WriteAssemblyDefinition writeAssemblyDefinition,
        GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, Delete delete, Move move,
        ModDefinition definition, TypePatcherInfo[] pluginPatchers
    ) {
        if (pluginPatchers.Length == 0) {
            return true;
        }

        logger.Information("Patching mod {ModId} ...", definition.Identifier);

        bool                success;
        var                 assemblyPath          = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
        var                 tempFilePath          = Path.ChangeExtension(assemblyPath, ".patched.dll");
        var                 shouldReplaceOriginal = false;
        AssemblyDefinition? assemblyDefinition    = null;
        try {
            var resolver = CreateAssemblyResolver(getCurrentDirectory, enumerateDirectories, definition, assemblyPath);

            var readParameters = new ReaderParameters { AssemblyResolver = resolver };
            assemblyDefinition = readAssemblyDefinition(assemblyPath, readParameters);
            if (assemblyDefinition == null) {
                logger.Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
                success = false;
            } else {
                (shouldReplaceOriginal, success) = TryPatchAssemblyDefinition(logger, definition, pluginPatchers, assemblyDefinition);

                if (shouldReplaceOriginal) {
                    writeAssemblyDefinition(assemblyDefinition, tempFilePath);
                    logger.Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", tempFilePath, definition.Identifier);
                } else {
                    logger.Information("No patches were applied to assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
                }
            }
        } finally {
            assemblyDefinition?.Dispose();
        }

        if (shouldReplaceOriginal) {
            try {
                delete(assemblyPath);
                move(tempFilePath, assemblyPath);
            } catch (Exception exc) {
                logger.Error(exc, "Failed to replace original assembly for mod {ModId}", definition.Identifier);
                success = false;
            }
        }

        if (!success) {
            logger.Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
        } else {
            logger.Information("Patching complete for mod {ModId}", definition.Identifier);
        }

        return success;
    }

    /// <summary>
    ///     Builds a <see cref="DefaultAssemblyResolver" /> that includes:
    ///     <list type="bullet">
    ///         <item>Game managed assemblies (<c>Railroader_Data\Managed</c>)</item>
    ///         <item>Only mods listed in <c>definition.Requires</c></item>
    ///     </list>
    /// </summary>
    private static DefaultAssemblyResolver CreateAssemblyResolver(GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, ModDefinition definition, string assemblyPath) {
        var resolver = new DefaultAssemblyResolver();
        resolver.RemoveSearchDirectory(".");
        resolver.RemoveSearchDirectory("bin");

        resolver.AddSearchDirectory(Path.Combine(getCurrentDirectory(), "Railroader_Data", "Managed"));

        //  stryker disable once block
        if (definition.Requires.Count == 0) {
            return resolver;
        }

        var thisModDir = Path.GetDirectoryName(assemblyPath);
        var modDirs = enumerateDirectories(Path.Combine(getCurrentDirectory(), "Mods"))
                      .Where(o => o != thisModDir)
                      .Where(o => definition.Requires.ContainsKey(Path.GetFileName(o)));
        foreach (var modDir in modDirs) {
            resolver.AddSearchDirectory(modDir);
        }

        return resolver;
    }

    /// <summary>
    ///     Applies all patchers that match implemented marker interfaces on each type.
    /// </summary>
    /// <returns>
    ///     <c>(shouldReplaceOriginal, success)</c> – whether a patched file should replace the original,
    ///     and whether the operation succeeded without errors.
    /// </returns>
    private static (bool shouldReplaceOriginal, bool success) TryPatchAssemblyDefinition(ILogger logger, ModDefinition definition, TypePatcherInfo[] pluginPatchers, AssemblyDefinition assemblyDefinition) {
        var hasPatch = false;
        var hasError = false;
        foreach (var type in assemblyDefinition.MainModule.Types) {
            try {
                var interfaces = type.Interfaces.Select(i => i.InterfaceType!.FullName).ToList();
                var patchers   = pluginPatchers.Where(pair => interfaces.Contains(pair.MarkerType.FullName)).Select(o => o.Factory());

                foreach (var patcher in patchers) {
                    hasPatch = true;
                    patcher!(assemblyDefinition, type);
                }
            } catch (Exception ex) {
                logger.Error(ex, "Failed to patch type {TypeName} for mod {ModId}", type.FullName, definition.Identifier);
                hasError = true;
            }
        }

        return (hasPatch && !hasError, !hasError);
    }
}