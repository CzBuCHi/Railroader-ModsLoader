using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Collections.Generic;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Railroader.ModManagerInstaller.Abstractions;

namespace Railroader.ModManagerInstaller.Tests.Utils;

public static class TestHelper
{
    public const string ManagedPath = @"C:\Game\Railroader_Data\Managed";

    public static void PrepareAppServices() {
        AppServices.Console = Substitute.For<IConsoleStatic>();
        AppServices.Assembly = Substitute.For<IAssemblyStatic>();
        AppServices.File = Substitute.For<IFileStatic>();
        AppServices.Directory = Substitute.For<IDirectoryStatic>();
        AppServices.Registry = Substitute.For<IRegistryStatic>();
        AppServices.ModuleDefinition = Substitute.For<IModuleDefinitionStatic>();

        AppServices.Directory.GetCurrentDirectory().Returns(@"C:\Game");
    }

    public static void VerifyReceivedCalls(Action[] calls) {
        Received.InOrder(() => {
            foreach (var action in calls) {
                action();
            }
        });
    }

    public static Action GetCurrentDirectoryCall => () => AppServices.Directory.GetCurrentDirectory();

    public static Action FileExistsCall(string fileName, bool exists) {
        var fullPath = Path.Combine(ManagedPath, fileName + ".dll");
        AppServices.File.Exists(fullPath).Returns(exists);
        return () => AppServices.File.Exists(fullPath);
    }

    private static void ConfigureCall(object call, object? valueOrException) {
        if (valueOrException is Exception exception) {
            call.Throws(exception);
        } else {
            call.Returns(valueOrException);
        }
    }

    public static Action ReadModuleCall(string fileName, IModuleDefinition moduleDefinition) => ReadModuleCall(fileName, (object)moduleDefinition);

    public static Action ReadModuleCall(string fileName, Exception exception) => ReadModuleCall(fileName, (object)exception);

    private static Action ReadModuleCall(string fileName, object moduleDefinitionOrException) {
        var fullPath = Path.Combine(ManagedPath, fileName + ".dll");

        var call = AppServices.ModuleDefinition.ReadModule(fullPath, Arg.Any<ReaderParameters>());
        ConfigureCall(call, moduleDefinitionOrException);
        return () => AppServices.ModuleDefinition.ReadModule(fullPath, Arg.Any<ReaderParameters>());
    }

    public static Action GetTypeCall(IModuleDefinition moduleDefinition, string fullName, TypeDefinition? typeDefinition) =>
        GetTypeCall(moduleDefinition, fullName, (object?)typeDefinition);

    public static Action GetTypeCall(IModuleDefinition moduleDefinition, string fullName, Exception exception) =>
        GetTypeCall(moduleDefinition, fullName, (object)exception);

    private static Action GetTypeCall(IModuleDefinition moduleDefinition, string fullName, object? valueOrException) {
        var call = moduleDefinition.GetType(fullName)!;
        ConfigureCall(call, valueOrException);
        return () => moduleDefinition.GetType(fullName);
    }

    public static IModuleDefinition CreateModuleDefinition(string name, params string[] assemblyReferences) {
        var moduleDefinition = Substitute.For<IModuleDefinition>();
        moduleDefinition.Assembly.Returns(
            AssemblyDefinition.CreateAssembly(
                new(name, new(1, 0)), name, ModuleKind.Dll
            )
        );

        var typeSystem = Substitute.For<ITypeSystem>();
        typeSystem.Void.Returns(new TypeReference("System", "Void", null!, null!));
        moduleDefinition.TypeSystem.Returns(_ => typeSystem);

        var assemblyNameReferences = new Collection<AssemblyNameReference>(
            assemblyReferences.Select(o => new AssemblyNameReference(o, new(1, 0))).ToArray()
        );
        moduleDefinition.AssemblyReferences.Returns(assemblyNameReferences);
        return moduleDefinition;
    }

    public static Action FindBootstrapMethod(TypeDefinition modManagerType) => () => _ = modManagerType.Methods.FirstOrDefault(m => m.Name == "Bootstrap");

    public static Action ImportReferenceCall(IModuleDefinition moduleDefinition, Exception exception) => ImportReferenceCall(moduleDefinition, (object)exception);

    public static Action ImportReferenceCall(IModuleDefinition moduleDefinition, MethodReference methodReference) => ImportReferenceCall(moduleDefinition, (object)methodReference);

    private static Action ImportReferenceCall(IModuleDefinition moduleDefinition, object methodReferenceOrException) {
        var call = moduleDefinition.ImportReference(Arg.Any<MethodReference>());
        ConfigureCall(call, methodReferenceOrException);
        return () => moduleDefinition.ImportReference(Arg.Any<MethodReference>());
    }

    public static Action StaticCtorCall(TypeDefinition typeDefinition) => () => _ = typeDefinition.Methods.FirstOrDefault(m => m.Name == ".cctor");

    public static IAssembly PrepareExecutingAssembly(string location) {
        var assembly = Substitute.For<IAssembly>();
        assembly.GetName().Returns(new AssemblyName("Installer") {
            Version = new(1, 2, 3)
        });
        assembly.Location.Returns(location);
        AppServices.Assembly.GetExecutingAssembly().Returns(assembly);
        return assembly;
    }
}
