using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Railroader.ModManagerInstaller.Abstractions;

namespace Railroader.ModManagerInstaller;

[PublicAPI]
public static class GameDirectoryResolver
{
    private const string Railroader = "Railroader.exe";

    public static Func<IAssembly, string?> TryResolveGameDirectory = TryResolveGameDirectoryCore;

    public static string? TryResolveGameDirectoryCore(IAssembly executingAssembly) {
        return CheckCurrentDirectory() ??
               CheckExecutingAssemblyLocation(executingAssembly) ??
               ResolveGameDirectoryFromRegistry();
    }

    public static string? CheckCurrentDirectory() {
        var currentDirectory = AppServices.Directory.GetCurrentDirectory();
        if (!AppServices.File.Exists(Path.Combine(currentDirectory, Railroader))) {
            return null;
        }

        AppServices.Console.WriteLine("Found Railroader in the current working directory.");
        return currentDirectory;
    }

    public static string? CheckExecutingAssemblyLocation(IAssembly executingAssembly) {
        var path = Path.GetDirectoryName(executingAssembly.Location)!;
        if (!AppServices.File.Exists(Path.Combine(path, Railroader))) {
            return null;
        }

        AppServices.Console.WriteLine($"Found Railroader in the {executingAssembly.GetName().Name} assembly directory.");
        return path;
    }
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static string? ResolveGameDirectoryFromRegistry() {
        using var registryKey = AppServices.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
        if (registryKey == null) {
            throw new ArgumentException("Cannot find Steam registry");
        }

        var steamPath = registryKey.GetValue("SteamPath") as string;
        if (steamPath == null || !AppServices.Directory.Exists(steamPath)) {
            throw new ArgumentException("Steam path not found, or does not exist on file system");
        }

        var ldf = VdfEntry.Load(Path.Combine(steamPath.TrimEnd(Path.DirectorySeparatorChar), "steamapps", "libraryfolders.vdf"));

        if (!ldf.TryGetValue("libraryfolders", out var libraryFoldersRaw) || libraryFoldersRaw is not VdfEntry libraryFolders) {
            return null;
        }

        foreach (var libraryFolder in libraryFolders.Values.OfType<VdfEntry>()) {
            if (libraryFolder.FindValue<VdfEntry>("apps")?.ContainsKey("1683150") != true) {
                continue;
            }

            var path = libraryFolder.FindValue<string>("path");
            return path != null
                ? Path.Combine(path, "steamapps", "common", "Railroader")
                : throw new VdfException("Path not found");
        }

        return null;
    }
}
