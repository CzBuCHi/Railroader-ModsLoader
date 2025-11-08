using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Railroader.ModManagerInstaller.Abstractions;

namespace Railroader.ModManagerInstaller;

[PublicAPI]
public static class Program
{
    public static void Main() {
        try {
            RunInstaller();
        } catch (InstallerException ex) {
            AppServices.Console.WriteLine(ex.Message!, ConsoleColor.Red);
        } catch (GamePathException ex) {
            AppServices.Console.WriteLine(ex.Message!, ConsoleColor.Red);
            AppServices.Console.WriteLine("Could not determine Railroader directory automatically.", ConsoleColor.Red);
            AppServices.Console.WriteLine("Move this installer into your game's directory, then run again.");
        } catch (Exception ex) {
            AppServices.Console.WriteLine("Unexpected error:", ConsoleColor.Red);
            AppServices.Console.WriteLine(ex.ToString(), ConsoleColor.Red);
        } finally {
            AppServices.Console.WriteLine("Press any key to exit.", ConsoleColor.White);
            AppServices.Console.ReadKey();
        }
    }

    private static void RunInstaller() {
        try {
            var assembly = AppServices.Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            AppServices.Console.Write(assemblyName + " ");
            AppServices.Console.WriteLine(assembly.GetName().Version, ConsoleColor.DarkGreen);
            AppServices.Console.SetTitle($"{assemblyName} {assembly.GetName().Version}");
        } catch (PlatformNotSupportedException) {
        }

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => ResolveInternalAssemblies(sender, args).Assembly;

        SetCurrentDirectory();
        ExtractFiles();
        Patcher.PatchGame();
        AppServices.Directory.CreateDirectory("Mods");

        AppServices.Console.WriteLine("Installation complete!", ConsoleColor.Green);
    }

    private static void ExtractFiles() {
        var prefix = typeof(Program).FullName!.Replace(nameof(Program), "Assemblies");

        string[] assemblies = [
            "0Harmony.dll",
            "Mono.Cecil.dll",
            "Mono.CSharp.dll",
            "Railroader.ModManager.dll",
            "Railroader.ModManager.Interfaces.dll"
        ];

        var executingAssembly = AppServices.Assembly.GetExecutingAssembly();


        AppServices.Console.WriteLine("Extracting files ...");
        foreach (var assembly in assemblies) {
            var path = Path.Combine("Railroader_Data", "Managed", assembly);

            AppServices.Console.WriteLine(path, ConsoleColor.DarkCyan);

            using var stream = executingAssembly.GetManifestResourceStream($"{prefix}.{assembly}")!;

            using var fileStream = AppServices.File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
            fileStream.SetLength(0L);
            stream.CopyTo(fileStream);
        }
    }

    private const string Railroader = "Railroader.exe";

    private static void SetCurrentDirectory() {
        if (AppServices.File.Exists(Path.Combine(AppServices.Directory.GetCurrentDirectory(), Railroader))) {
            AppServices.Console.WriteLine("Found Railroader in the current working directory.");
            return;
        }

        var executingAssembly = AppServices.Assembly.GetExecutingAssembly();
        var path = Path.GetDirectoryName(executingAssembly.Location)!;
        if (AppServices.File.Exists(Path.Combine(path, Railroader))) {
            AppServices.Console.WriteLine($"Found Railroader in the {executingAssembly.GetName().Name} assembly directory.");
            AppServices.Directory.SetCurrentDirectory(path);
            return;
        }

        path = FindRailroaderFromRegistry();
        if (path == null) {
            throw new GamePathException($"Could not find {Railroader} using Steam's Library.");
        }

        if (AppServices.File.Exists(Path.Combine(path, Railroader))) {
            AppServices.Console.WriteLine("Found Railroader using Steam's Library.");
            AppServices.Directory.SetCurrentDirectory(path);
            return;
        }

        throw new GamePathException($"Could not find {Railroader} (Steam's Library path is invalid).");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static string? FindRailroaderFromRegistry() {
        var steamIdRegex = new Regex("^\\s*\"1683150\"\\s*\"\\d+\"\\s*$", RegexOptions.Compiled);
        var pathRegex = new Regex("^\\s*\"path\"\\s*\"(.+?)\"\\s*$", RegexOptions.Compiled);

        using var registryKey = AppServices.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
        if (registryKey == null) {
            throw new ArgumentException("Cannot find Steam registry");
        }

        if (registryKey.GetValue("SteamPath") is not string text || !AppServices.Directory.Exists(text)) {
            throw new ArgumentException("Steam path not found, or does not exist on file system");
        }

        var array = AppServices.File.ReadAllLines(Path.Combine(text.TrimEnd('/', '\\'), "steamapps", "libraryfolders.vdf"));
        for (var i = 0; i < array.Length; i++) {
            var input = array[i];
            if (!steamIdRegex.IsMatch(input)) {
                continue;
            }

            for (var num = i - 1; num > 0; num--) {
                input = array[num];
                if (input == "}") {
                    throw new ArgumentException("Found delimiter instead of path.");
                }

                var match = pathRegex.Match(input);
                if (!match.Success) {
                    continue;
                }

                var path = Path.Combine(match.Groups[1].Value.Replace(@"\\", "\\").TrimEnd('/', '\\'), "steamapps", "common", "Railroader");
                if (!AppServices.File.Exists(Path.Combine(path, Railroader))) {
                    throw new ArgumentException($"{Railroader} not found at the specified location");
                }

                return path;
            }
        }

        return null;
    }

    private static IAssembly ResolveInternalAssemblies(object sender, ResolveEventArgs args) {
        var name = new AssemblyName(args.Name!).Name;
        if (!name.StartsWith("Mono.Cecil") && !name.StartsWith("Newtonsoft.Json")) {
            throw new InstallerException($"Could not load missing assembly: {name}");
        }

        var stream = typeof(Program).Assembly.GetManifestResourceStream($"Assemblies/{name}.dll")
                     ?? throw new InstallerException($"Embedded assembly not found: {name}.dll");

        var buffer = new byte[stream.Length];
        return stream.Read(buffer, 0, buffer.Length) != buffer.Length
            ? throw new InstallerException($"Failed to read embedded assembly: {name}.dll")
            : AppServices.Assembly.Load(buffer);
    }
}