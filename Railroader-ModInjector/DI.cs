using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
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

    public delegate T Factory<out T>();

    public delegate T Factory<out T, in TArg>(TArg arg);

    public delegate ILogger GetLoggerDelegate(string? scope = null);

    public static Action<LoggerConfiguration> CreateLogger { get; set; } = configuration => Logger = configuration.CreateLogger()!;

    public static GetLoggerDelegate GetLogger { get; set; } = scope => Logger.ForContext("SourceContext", scope ?? "Railroader.ModInjector")!;

    public static Factory<IAssemblyCompiler> AssemblyCompiler { get; set; } =
        () => new AssemblyCompiler {
            CompilerCallableEntryPoint = CompilerCallableEntryPoint(),
            Logger = GetLogger()
        };

    public static Factory<IAssemblyDefinitionWrapper> AssemblyDefinitionWrapper { get; set; } =
        () => new AssemblyDefinitionWrapper();

    public static Factory<IAssemblyWrapper> AssemblyWrapper { get; set; } =
        () => new AssemblyWrapper();

    public static Factory<ICodeCompiler> CodeCompiler { get; set; } =
        () => new CodeCompiler {
            FileSystem = FileSystem(),
            Logger = GetLogger(),
            AssemblyDefinitionWrapper = AssemblyDefinitionWrapper(),
            AssemblyCompiler = AssemblyCompiler()
        };

    public static Factory<ICompilerCallableEntryPoint> CompilerCallableEntryPoint { get; set; } =
        () => new CompilerCallableEntryPointWrapper();

    public static Factory<IFileSystem> FileSystem { get; set; } =
        () => new FileSystemWrapper {
            Logger = GetLogger()
        };

    public static Factory<ILogConfigurator> LogConfigurator { get; set; } =
        () => new LogConfigurator();

    public static Factory<IModDefinitionLoader> ModDefinitionLoader { get; set; } =
        () => new ModDefinitionLoader {
            Logger = GetLogger(),
            FileSystem = FileSystem()
        };

    public static Factory<IModManager> ModManager { get; set; } =
        () => new ModManager {
            CodeCompiler = CodeCompiler(),
            PluginManagerFactory = o => PluginManager(o)
        };

    public static Factory<IPluginManager, ModdingContext> PluginManager { get; set; } =
        context => new PluginManager {
            AssemblyWrapper = AssemblyWrapper(),
            ModdingContext = context,
            Logger = GetLogger()
        };
}
