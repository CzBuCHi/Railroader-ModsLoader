using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Railroader.ModManagerInstaller.Abstractions;

namespace Railroader.ModManagerInstaller;

[PublicAPI]
[ExcludeFromCodeCoverage]
public static class AppServices
{
    public static IConsoleStatic Console = new ConsoleStatic();
    public static IAssemblyStatic Assembly = new AssemblyStatic();
    public static IFileStatic File = new FileStatic();
    public static IDirectoryStatic Directory = new DirectoryStatic();
    public static IRegistryStatic Registry = new RegistryStatic();
    public static IModuleDefinitionStatic ModuleDefinition = new ModuleDefinitionStatic();
}