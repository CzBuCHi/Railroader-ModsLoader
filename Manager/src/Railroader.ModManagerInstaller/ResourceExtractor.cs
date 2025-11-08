using System;
using System.IO;
using JetBrains.Annotations;
using Railroader.ModManagerInstaller.Abstractions;

namespace Railroader.ModManagerInstaller;

[PublicAPI]
public static class ResourceExtractor
{
    public static Action<IAssembly> ExtractFiles = ExtractFilesCore;
    
    public static void ExtractFilesCore(IAssembly executingAssembly) {
        var prefix = typeof(Program).FullName!.Replace(nameof(Program), "Assemblies");

        string[] assemblies = [
            "0Harmony.dll",
            "Mono.Cecil.dll",
            "Mono.CSharp.dll",
            "Railroader.ModManager.dll",
            "Railroader.ModManager.Interfaces.dll"
        ];

        AppServices.Console.WriteLine("Extracting files ...");
        foreach (var assembly in assemblies) {
            var path = Path.Combine(AppServices.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed", assembly);

            AppServices.Console.WriteLine(path, ConsoleColor.DarkCyan);

            using var stream = executingAssembly.GetManifestResourceStream($"{prefix}.{assembly}")!;

            using var fileStream = AppServices.File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
            stream.CopyTo(fileStream);
        }
    }
}
