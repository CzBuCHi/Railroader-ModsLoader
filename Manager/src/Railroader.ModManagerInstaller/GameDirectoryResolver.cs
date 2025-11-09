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

        if (registryKey.GetValue("SteamPath") is not string steamPath || !AppServices.Directory.Exists(steamPath)) {
            throw new ArgumentException("Steam path not found, or does not exist on file system");
        }

        var ldf = VdfEntry.Load(Path.Combine(steamPath.TrimEnd(Path.DirectorySeparatorChar), "steamapps", "libraryfolders.vdf"));

        if (!ldf.TryGetValue("libraryfolders", out var libraryFoldersRaw) || libraryFoldersRaw is not VdfEntry libraryFolders) {
            return null;
        }

        foreach (var libraryFolder in libraryFolders.Values.OfType<VdfEntry>()) {
            if (!libraryFolder.TryGetValue("apps", out var appsRaw) || appsRaw is not VdfEntry apps) {
                continue;
            }

            if (!apps.ContainsKey("1683150")) {
                continue;
            }

            if (!libraryFolder.TryGetValue("path", out var pathRaw) || pathRaw is not string path) {
                throw new VdfException("Path is not string");
            }

            return Path.Combine(path, "steamapps", "common", "Railroader");
        }

        return null;
    }
}
