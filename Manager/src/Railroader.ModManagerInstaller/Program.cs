using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Railroader.ModManagerInstaller;

public static class Program
{
    private static readonly Assembly _Assembly     = Assembly.GetExecutingAssembly();
    private static readonly string   _AssemblyName = _Assembly.GetName().Name;

    public static void Main() {
        try {
            Console.Write(_AssemblyName);
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(_Assembly.GetName().Version);
            Console.ResetColor();
            Console.Title = $"{_AssemblyName} {_Assembly.GetName().Version}";
        } catch (PlatformNotSupportedException) {
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveInternalAssemblies;

        if (!SetCurrentDirectory()) {
            ConsoleEx.WriteError("Could not determine Railroader directory automatically.");
            Console.WriteLine($"Move this {_AssemblyName} into your game's directory, then run again.");
            Environment.Exit(1);
        }

        try {
            ExtractFiles();
            Patcher.PatchGame();
            Directory.CreateDirectory("Mods");
        } catch (Exception exc) {
            ConsoleEx.WriteError("Failed to patch game.");
            Console.Error.WriteLine(exc);
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.Error.WriteLine("Press any key to exit.");
        Console.ReadKey();
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

        Console.WriteLine("Extracting files ...");
        foreach (var assembly in assemblies) {
            var path = Path.Combine("Railroader_Data", "Managed", assembly);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Error.WriteLine(path);
            Console.ResetColor();

            using var stream     = _Assembly.GetManifestResourceStream($"{prefix}.{assembly}")!;
            using var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            fileStream.SetLength(0L);
            stream.CopyTo(fileStream);
        }
    }

    private const string Railroader = "Railroader.exe";

    private static bool SetCurrentDirectory() {
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, Railroader))) {
            Console.WriteLine("Found Railroader in the current working directory.");
            return true;
        }

        var path = Path.GetDirectoryName(_Assembly.Location)!;
        if (File.Exists(Path.Combine(path, Railroader))) {
            Console.WriteLine($"Found Railroader in the {_AssemblyName} assembly directory.");
            Environment.CurrentDirectory = path;
            return true;
        }

        path = FindRailroaderFromRegistry();
        if (path == null) {
            ConsoleEx.WriteError($"Could not find {Railroader} using Steam's Library.");
            return false;
        }

        if (File.Exists(Path.Combine(path, Railroader))) {
            Console.WriteLine("Found Railroader using Steam's Library.");
            Environment.CurrentDirectory = path;
            return true;
        }

        ConsoleEx.WriteError($"Could not find {Railroader} (Steam's Library path is invalid).");
        return false;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static string? FindRailroaderFromRegistry() {
        var steamIdRegex = new Regex("^\\s*\"1683150\"\\s*\"\\d+\"\\s*$", RegexOptions.Compiled);
        var pathRegex    = new Regex("^\\s*\"path\"\\s*\"(.+?)\"\\s*$", RegexOptions.Compiled);

        using var registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
        if (registryKey == null) {
            throw new ArgumentException("Cannot find Steam registry");
        }

        if (registryKey.GetValue("SteamPath") is not string text || !Directory.Exists(text)) {
            throw new ArgumentException("Steam path not found, or does not exist on file system");
        }

        string[] array = File.ReadAllLines(Path.Combine(text.TrimEnd('/', '\\'), "steamapps", "libraryfolders.vdf"));
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
                if (!File.Exists(Path.Combine(path, Railroader))) {
                    throw new ArgumentException($"{Railroader} not found at the specified location");
                }

                return path;
            }
        }

        return null;
    }

    private static Assembly? ResolveInternalAssemblies(object sender, ResolveEventArgs args) {
        var name         = args.Name!;
        var assemblyName = new AssemblyName(name);
        if (name.StartsWith("Mono.Cecil") || name.StartsWith("Newtonsoft.Json")) {
            var manifestResourceStream = typeof(Program).Assembly.GetManifestResourceStream($"Assemblies/{assemblyName.Name}.dll");
            if (manifestResourceStream != null) {
                var array = new byte[manifestResourceStream.Length];
                var size  = manifestResourceStream.Read(array, 0, array.Length);
                if (size == array.Length) {
                    return Assembly.Load(array);
                }
            }
        }

        ConsoleEx.WriteFatal($"Could not load missing assembly {assemblyName}");
        return null;
    }
}
