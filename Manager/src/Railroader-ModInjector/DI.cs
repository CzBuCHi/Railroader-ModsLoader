using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInjector.Wrappers.FileSystem;
using Serilog;

namespace Railroader.ModInjector;

/// <summary> Factory method for all manager services. </summary>
/// <remarks> Whole purpose of this is ability to replace any service with mock in test</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[PublicAPI]
[ExcludeFromCodeCoverage]
internal static class DI
{
    /// <summary> Serilog logger instance. </summary>
    public static ILogger Logger { get; set; } = new InitLogger();
    
    public delegate ILogger GetLoggerDelegate(string? scope = null);

    public static Action<LoggerConfiguration> CreateLogger { get; set; } = configuration => Logger = configuration.CreateLogger()!;

    public static GetLoggerDelegate GetLogger { get; set; } = scope => Logger.ForContext("SourceContext", scope ?? "Railroader.ModInjector")!;

    public static Func<ICompilerCallableEntryPoint> CompilerCallableEntryPoint { get; set; } =
        () => new CompilerCallableEntryPointWrapper();

    public static Func<IAssemblyCompiler> AssemblyCompiler { get; set; } =
        () => new AssemblyCompiler {
            CompilerCallableEntryPoint = CompilerCallableEntryPoint()!,
            Logger = GetLogger()
        };

    public static Func<IAssemblyDefinitionWrapper> AssemblyDefinitionWrapper { get; set; } =
        () => new AssemblyDefinitionWrapper();

    public static Func<IAssemblyWrapper> AssemblyWrapper { get; set; } =
        () => new AssemblyWrapper();

    public static Func<IFileSystem> FileSystem { get; set; } =
        () => new FileSystemWrapper();

    public static Func<ICodeCompiler> CodeCompiler { get; set; } =
        () => new CodeCompiler {
            FileSystem = FileSystem()!,
            Logger = GetLogger(),
            AssemblyDefinitionWrapper = AssemblyDefinitionWrapper()!,
            AssemblyCompiler = AssemblyCompiler()!
        };

    public static Func<string, IHarmonyWrapper> HarmonyWrapper { get; set; } =
        id => new HarmonyWrapper(id);

    public static Func<ILogConfigurator> LogConfigurator { get; set; } =
        () => new LogConfigurator();

    public static Func<IModDefinitionLoader> ModDefinitionLoader { get; set; } =
        () => new ModDefinitionLoader {
            Logger = GetLogger(),
            FileSystem = FileSystem()!
        };

    public static Func<ModdingContext, IPluginManager> PluginManager { get; set; } =
        context => new PluginManager {
            AssemblyWrapper = AssemblyWrapper()!,
            ModdingContext = context,
            Logger = GetLogger()
        };

    public static Func<IModDefinitionProcessor> ModDefinitionProcessor { get; set; } = () => new ModDefinitionProcessor();

    public static Func<IModExtractorService> ModExtractorService { get; set; } = () => new ModExtractorService {
        FileSystem = FileSystem()!,
        Logger = GetLogger()
    };

    

    public static Func<IModManager> ModManager { get; set; } =
        () => new ModManager {
            CodeCompiler = CodeCompiler()!,
            PluginManagerFactory = o => PluginManager(o)!,
            ModDefinitionProcessor = ModDefinitionProcessor()!,
            ModExtractorService = ModExtractorService()!,
        };


}
