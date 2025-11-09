using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Features;
using Railroader.ModManager.Services;
using Serilog;

// run mod manager without running the game, uses fake game folder

Directory.SetCurrentDirectory(@"c:\projects\Railroader\Railroader-ModsLoader\HostRoot\");

var memoryLogger = new MemoryLogger();
Log.Logger = memoryLogger;

Bootstrapper.Execute(
    ModExtractor.GetExtractor(memoryLogger),
    ModDefinitionLoader.Create(memoryLogger),
    HarmonyWrapper.Factory("Railroader.ModManager"),
    Bootstrapper.LoadMods
);

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

memoryLogger.Flush(logger);

Console.WriteLine();
Console.WriteLine("DONE");
Console.ReadKey();

[ExcludeFromCodeCoverage]
public static partial class Program;


