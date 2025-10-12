using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Railroader.ModsLoader;

public static class Program
{
    private static readonly Assembly _Assembly     = Assembly.GetExecutingAssembly();
    private static readonly string?  _AssemblyName = _Assembly.GetName().Name;

    private const string Railroader = "Railroader.exe";

    public static void Main() {
        try {
            var text = $"{_AssemblyName} {_Assembly.GetName().Version}";
            Console.WriteLine(text);
            Console.Title = text;
        } catch (PlatformNotSupportedException) {
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveInternalAssemblies;

        if (!SetCurrentDirectory()) {
            WriteError("Could not determine Railroader directory automatically.");
            Console.WriteLine($"Move this {_AssemblyName} into your game's directory, then run again.");
            Environment.Exit(1);
        }

        if (Patcher.PatchGame()) {
            ExtractFiles();
        }

        if (!Directory.Exists("Mods")) {
            Directory.CreateDirectory("Mods");
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.Error.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    private static void ExtractFiles() {
        const string injector = "Railroader.ModsLoader.Injector.";

        Console.WriteLine("Extracting files ...");
        foreach (var item in _Assembly.GetManifestResourceNames().Where(n => n.StartsWith(injector))) {
            var path = item.Replace(injector, "Railroader_Data/Managed/");

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Error.WriteLine(path);
            Console.ResetColor();

            using var stream     = _Assembly.GetManifestResourceStream(item)!;
            using var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            fileStream.SetLength(0L);
            stream.CopyTo(fileStream);
        }
    }

    private static Assembly? ResolveInternalAssemblies(object sender, ResolveEventArgs args) {
        var name         = args.Name!;
        var assemblyName = new AssemblyName(name);
        if (name.StartsWith("Mono.Cecil") || name.StartsWith("Newtonsoft.Json")) {
            var manifestResourceStream = typeof(Program).Assembly.GetManifestResourceStream($"Assemblies/{assemblyName.Name}.dll")!;
            if (manifestResourceStream != null!) {
                var array = new byte[manifestResourceStream.Length];
                var size  = manifestResourceStream.Read(array, 0, array.Length);
                if (size == array.Length) {
                    return Assembly.Load(array);
                }
            }
        }

        WriteFatal($"Could not load missing assembly {assemblyName}");
        return null;
    }

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
            WriteError($"Could not find {Railroader} using Steam's Library.");
            return false;
        }

        if (File.Exists(Path.Combine(path, Railroader))) {
            Console.WriteLine("Found Railroader using Steam's Library.");
            Environment.CurrentDirectory = path;
            return true;
        }

        WriteError($"Could not find {Railroader} (Steam's Library path is invalid).");
        return false;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static string? FindRailroaderFromRegistry() {
        var steamIdRegex = new Regex("^\\s*\"1683150\"\\s*\"\\d+\"\\s*$", RegexOptions.Compiled);
        var pathRegex    = new Regex("^\\s*\"path\"\\s*\"(.+?)\"\\s*$", RegexOptions.Compiled);

        using var registryKey = Registry.CurrentUser!.OpenSubKey(@"SOFTWARE\Valve\Steam");
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

    private static void WriteError(string message) => WriteLine(message, ConsoleColor.Red);

    public static void WriteWarning(string message) => WriteLine(message, ConsoleColor.DarkYellow);

    private static void WriteLine(string message, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteFatal(string message) {
        WriteError(message);
        Environment.Exit(1);
    }
}
