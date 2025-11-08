using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Railroader.ModManagerInstaller.Abstractions;

namespace Railroader.ModManagerInstaller;

[PublicAPI]
public static class Program
{
    private const string Railroader = "Railroader.exe";
    
    public static void Main() {
        try {
            RunInstaller();
        } catch (InstallerException ex) {
            AppServices.Console.WriteLine(ex.Message!, ConsoleColor.Red);
            if (ex.InnerException != null) {
                AppServices.Console.WriteLine(" - " + ex.InnerException.Message!, ConsoleColor.Red);
            }
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

    private static IAssembly _ExecutingAssembly = null!;
    
    internal static void RunInstaller() {
        try {
            _ExecutingAssembly = AppServices.Assembly.GetExecutingAssembly();
            var assemblyName = _ExecutingAssembly.GetName().Name;
            AppServices.Console.Write(assemblyName + " ");
            AppServices.Console.WriteLine(_ExecutingAssembly.GetName().Version, ConsoleColor.DarkGreen);
            AppServices.Console.SetTitle($"{assemblyName} {_ExecutingAssembly.GetName().Version}");
        } catch (PlatformNotSupportedException) {
        }

        // stryker disable once assignment
        AppDomain.CurrentDomain.AssemblyResolve += ResolveInternalAssembliesHandler;

        ResolveGameDirectory();
        ResourceExtractor.ExtractFiles(_ExecutingAssembly);
        Patcher.PatchGame();
        AppServices.Directory.CreateDirectory("Mods");

        AppServices.Console.WriteLine("Installation complete!", ConsoleColor.DarkGreen);
    }

    private static void ResolveGameDirectory() {
        var path = GameDirectoryResolver.TryResolveGameDirectory(_ExecutingAssembly);
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

    [ExcludeFromCodeCoverage]
    private static Assembly ResolveInternalAssembliesHandler(object sender, ResolveEventArgs args) => ResolveInternalAssemblies(sender, args).Assembly;

    public static IAssembly ResolveInternalAssemblies(object sender, ResolveEventArgs args) {
        var name = new AssemblyName(args.Name!).Name;
        if (!name.StartsWith("Mono.Cecil") && !name.StartsWith("Newtonsoft.Json")) {
            throw new InstallerException($"Could not load missing assembly: {name}");
        }

        var executingAssembly = AppServices.Assembly.GetExecutingAssembly();
        var stream = executingAssembly.GetManifestResourceStream($"Assemblies/{name}.dll")
                     ?? throw new InstallerException($"Embedded assembly not found: {name}.dll");

        var buffer = new byte[stream.Length];
        _ = stream.Read(buffer, 0, buffer.Length);
        return AppServices.Assembly.Load(buffer);
    }
}