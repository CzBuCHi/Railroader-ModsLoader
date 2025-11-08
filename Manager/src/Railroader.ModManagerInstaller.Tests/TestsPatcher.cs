using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NSubstitute;
using Railroader.ModManagerInstaller.Tests.Utils;
using Shouldly;

namespace Railroader.ModManagerInstaller.Tests;

[Collection("ModManagerInstaller")]
public sealed class TestsPatcher
{
    private const string ModManagerInterfaces = "Railroader.ModManager.Interfaces";
    private const string ModManager = "Railroader.ModManager";
    private const string ModManagerType = "Railroader.ModManager.ModManager";
    private const string AssemblyCsharpDll = "Assembly-CSharp";

    [Fact]
    public void WhenModManagerInterfacesDllNotFound_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();
        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, false)
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<InstallerException>()
            .Message.ShouldBe($"Could not locate file '{ModManagerInterfaces}.dll'.");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenModManagerDllNotFound_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();
        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, false)
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<InstallerException>()
            .Message.ShouldBe($"Could not locate file '{ModManager}.dll'.");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenAssemblyCsharpDllNotFound_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();
        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, false)
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<InstallerException>()
            .Message.ShouldBe($"Could not locate file '{AssemblyCsharpDll}.dll'.");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenReadModuleThrows_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, new InstallerException("Simulated ReadModule failure"))
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<InstallerException>()
            .Message!.ShouldContain("Simulated ReadModule failure");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenAlreadyPatched_WritesAlreadyPatchedMessageAndExits() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll, ModManager);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            () => AppServices.Console.WriteLine("Railroader is already patched.", ConsoleColor.DarkYellow)
        ];

        // Act
        Patcher.PatchGame();

        // Assert
        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenInjectModuleFailsOnModManagerInterfaces_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, new InstallerException("Simulated ReadModule failure"))
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        var ex = act.ShouldThrow<InstallerException>();
        ex.Message.ShouldBe("Simulated ReadModule failure");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenInjectModManagerInterfacesSucceeds_AddsAssemblyReference() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, new TestSuicide())
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<TestSuicide>();

        TestHelper.VerifyReceivedCalls(calls);

        csharpModule.AssemblyReferences.ShouldContain(o => o.Name == ModManagerInterfaces);
    }

    [Fact]
    public void WhenInjectModuleFailsOnModManager_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, new InstallerException("Simulated ReadModule failure"))
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        var ex = act.ShouldThrow<InstallerException>();
        ex.Message.ShouldBe("Simulated ReadModule failure");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenInjectModManagerSucceeds_AddsAssemblyReference() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerModule = TestHelper.CreateModuleDefinition(ModManager);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, modManagerModule),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", new TestSuicide())
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<TestSuicide>();

        TestHelper.VerifyReceivedCalls(calls);

        csharpModule.AssemblyReferences.ShouldContain(o => o.Name == ModManager);
    }

    [Fact]
    public void WhenLogManagerTypeMissing_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerModule = TestHelper.CreateModuleDefinition(ModManager);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, modManagerModule),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", (TypeDefinition?)null)
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        var ex = act.ShouldThrow<InstallerException>();
        ex.Message.ShouldBe("Could not find Logging.LogManager type.");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenModManagerTypeMissing_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerModule = TestHelper.CreateModuleDefinition(ModManager);

        var logManager = new TypeDefinition("Logging", "LogManager", TypeAttributes.Class);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, modManagerModule),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", logManager),
            TestHelper.GetTypeCall(modManagerModule, ModManagerType, (TypeDefinition?)null)
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        var ex = act.ShouldThrow<InstallerException>();
        ex.Message.ShouldBe($"Could not find {ModManagerType} type.");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenBootstrapMethodMissing_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerModule = TestHelper.CreateModuleDefinition(ModManager);

        var logManagerType = new TypeDefinition("Logging", "LogManager", TypeAttributes.Class);
        var modManagerType = new TypeDefinition("Railroader.ModManager", "ModManager", TypeAttributes.Class);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, modManagerModule),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", logManagerType),
            TestHelper.GetTypeCall(modManagerModule, ModManager + ".ModManager", modManagerType),
            TestHelper.FindBootstrapMethod(modManagerType)
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        var ex = act.ShouldThrow<InstallerException>();
        ex.Message.ShouldBe("Could not find Railroader.ModManager.ModManager.Bootstrap method.");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenImportReferenceThrows_ThrowsInstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerModule = TestHelper.CreateModuleDefinition(ModManager);

        var logManagerType = new TypeDefinition("Logging", "LogManager", TypeAttributes.Class);
        var modManagerType = new TypeDefinition("Railroader.ModManager", "ModManager", TypeAttributes.Class);
        var bootstrapMethod = new MethodDefinition("Bootstrap", MethodAttributes.Public | MethodAttributes.Static, csharpModule.TypeSystem.Void);
        modManagerType.Methods.Add(bootstrapMethod);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, modManagerModule),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", logManagerType),
            TestHelper.GetTypeCall(modManagerModule, ModManager + ".ModManager", modManagerType),
            TestHelper.FindBootstrapMethod(modManagerType),
            TestHelper.ImportReferenceCall(csharpModule, new InstallerException("Could not find Railroader.ModManager.ModManager.Bootstrap method."))
        ];

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<InstallerException>()
            .Message.ShouldBe("Could not find Railroader.ModManager.ModManager.Bootstrap method.");

        TestHelper.VerifyReceivedCalls(calls);
    }

    [Fact]
    public void WhenCctorAlreadyCallsBootstrap_SkipsPatch() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerModule = TestHelper.CreateModuleDefinition(ModManager);

        var logManagerType = new TypeDefinition("Logging", "LogManager", TypeAttributes.Class);
        var modManagerType = new TypeDefinition("Railroader.ModManager", "ModManager", TypeAttributes.Class);
        var bootstrapMethod = new MethodDefinition("Bootstrap", MethodAttributes.Public | MethodAttributes.Static, csharpModule.TypeSystem.Void);
        modManagerType.Methods.Add(bootstrapMethod);

        var logManagerCctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static, csharpModule.TypeSystem.Void);
        var il = logManagerCctor.Body.GetILProcessor();
        var importedBootstrap = new MethodReference("Bootstrap", csharpModule.TypeSystem.Void, modManagerType) { HasThis = false };
        il.Emit(OpCodes.Call, importedBootstrap);
        il.Emit(OpCodes.Ret);
        logManagerType.Methods.Add(logManagerCctor);

        TestHelper.StaticCtorCall(logManagerType);

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, modManagerModule),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", logManagerType),
            TestHelper.GetTypeCall(modManagerModule, ModManagerType, modManagerType),
            TestHelper.FindBootstrapMethod(modManagerType),
            TestHelper.ImportReferenceCall(csharpModule, importedBootstrap),
            () => AppServices.Console.WriteLine("Logging.LogManager static constructor already calls ModManager.Bootstrap. Skipping patch.",
                ConsoleColor.DarkYellow)
        ];

        // Act
        Patcher.PatchGame();

        // Assert
        TestHelper.VerifyReceivedCalls(calls);

        // No IL changes
        logManagerCctor.Body.Instructions.Count.ShouldBe(2); // call + ret
        logManagerCctor.Body.Instructions[0]!.OpCode.ShouldBe(OpCodes.Call);
        logManagerCctor.Body.Instructions[0]!.Operand.ShouldBe(importedBootstrap);

        // No write
        csharpModule.DidNotReceive().Write(Arg.Any<string>());
    }

    [Fact]
    public void WhenCctorNotFound_CreatesCctorAndPatches() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesMod = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerMod = TestHelper.CreateModuleDefinition(ModManager);

        var logManagerType = new TypeDefinition("Logging", "LogManager", TypeAttributes.Class);
        var modManagerType = new TypeDefinition("Railroader.ModManager", "ModManager", TypeAttributes.Class);
        var bootstrapMethod = new MethodDefinition("Bootstrap", MethodAttributes.Public | MethodAttributes.Static, csharpModule.TypeSystem.Void);
        modManagerType.Methods.Add(bootstrapMethod);

        var importedBootstrap = new MethodReference("Bootstrap", csharpModule.TypeSystem.Void, modManagerType) { HasThis = false };
        TestHelper.ImportReferenceCall(csharpModule, importedBootstrap);

        var assemblyPath = Path.Combine(TestHelper.ManagedPath, AssemblyCsharpDll + ".dll");
        var backupPath = assemblyPath.Replace(".dll", "_original.dll");

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesMod),
            TestHelper.ReadModuleCall(ModManager, modManagerMod),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", logManagerType),
            TestHelper.GetTypeCall(modManagerMod, ModManagerType, modManagerType),
            TestHelper.FindBootstrapMethod(modManagerType),
            TestHelper.ImportReferenceCall(csharpModule, importedBootstrap),
            () => _ = logManagerType.Methods.FirstOrDefault(m => m.Name == ".cctor"),
            () => AppServices.File.Copy(assemblyPath, backupPath),
            () => csharpModule.Write(assemblyPath),
            () => AppServices.File.SetLastWriteTime(backupPath, Arg.Any<DateTime>()),
            () => AppServices.Console.WriteLine("Successfully patched game.")
        ];

        // Act
        Patcher.PatchGame();

        // Assert
        TestHelper.VerifyReceivedCalls(calls);

        var cctor = logManagerType.Methods.FirstOrDefault(m => m.Name == ".cctor");
        cctor.ShouldNotBeNull();
        cctor.Body.Instructions.Count.ShouldBe(2);

        // First instruction: call Bootstrap
        cctor.Body.Instructions[0]!.OpCode.ShouldBe(OpCodes.Call);
        cctor.Body.Instructions[0]!.Operand.ShouldBe(importedBootstrap);

        // Second: ret
        cctor.Body.Instructions[1]!.OpCode.ShouldBe(OpCodes.Ret);
    }

    [Fact]
    public void WhenCctorExistsButEmpty_InsertsCallBeforeRet() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesMod = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerMod = TestHelper.CreateModuleDefinition(ModManager);

        var logManagerType = new TypeDefinition("Logging", "LogManager", TypeAttributes.Class);
        var modManagerType = new TypeDefinition("Railroader.ModManager", "ModManager", TypeAttributes.Class);
        var bootstrapMethod = new MethodDefinition("Bootstrap", MethodAttributes.Public | MethodAttributes.Static, csharpModule.TypeSystem.Void);
        modManagerType.Methods.Add(bootstrapMethod);

        // Create empty .cctor with just 'ret'
        var cctor = new MethodDefinition(".cctor",
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            csharpModule.TypeSystem.Void);
        var il = cctor.Body.GetILProcessor();
        il.Append(il.Create(OpCodes.Ret));
        logManagerType.Methods.Add(cctor);

        var importedBootstrap = new MethodReference("Bootstrap", csharpModule.TypeSystem.Void, modManagerType) { HasThis = false };

        var assemblyPath = Path.Combine(TestHelper.ManagedPath, AssemblyCsharpDll + ".dll");
        var backupPath = assemblyPath.Replace(".dll", "_original.dll");

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesMod),
            TestHelper.ReadModuleCall(ModManager, modManagerMod),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", logManagerType),
            TestHelper.GetTypeCall(modManagerMod, ModManagerType, modManagerType),
            TestHelper.FindBootstrapMethod(modManagerType),
            TestHelper.ImportReferenceCall(csharpModule, importedBootstrap),
            () => _ = logManagerType.Methods.FirstOrDefault(m => m.Name == ".cctor"),
            () => _ = cctor.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand == importedBootstrap),
            () => AppServices.File.Copy(assemblyPath, backupPath),
            () => csharpModule.Write(assemblyPath),
            () => AppServices.File.SetLastWriteTime(backupPath, Arg.Any<DateTime>()),
            () => AppServices.Console.WriteLine("Successfully patched game.")
        ];

        // Act
        Patcher.PatchGame();

        // Assert
        TestHelper.VerifyReceivedCalls(calls);

        cctor.Body.Instructions.Count.ShouldBe(2);
        cctor.Body.Instructions[0]!.OpCode.ShouldBe(OpCodes.Call);
        cctor.Body.Instructions[0]!.Operand.ShouldBe(importedBootstrap);
        cctor.Body.Instructions[1]!.OpCode.ShouldBe(OpCodes.Ret);
    }

    [Fact]
    public void WhenAssemblyResolverNotConfigured_ReadModuleThrows() {
        // Arrange
        var assemblyCsharpPath = Path.Combine(TestHelper.ManagedPath, AssemblyCsharpDll + ".dll");

        TestHelper.PrepareAppServices();
        TestHelper.FileExistsCall(ModManagerInterfaces, true);
        TestHelper.FileExistsCall(ModManager, true);
        TestHelper.FileExistsCall(AssemblyCsharpDll, true);
        TestHelper.ReadModuleCall(AssemblyCsharpDll, new TestSuicide());

        // Act
        var act = Patcher.PatchGame;

        // Assert
        act.ShouldThrow<TestSuicide>();

        AppServices.ModuleDefinition.Received().ReadModule(assemblyCsharpPath, Arg.Is<ReaderParameters>(o =>
            o.InMemory == true &&
            o.AssemblyResolver is DefaultAssemblyResolver && ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!
            .SequenceEqual(new[] { TestHelper.ManagedPath })
        ));
    }

    [Fact]
    public void WhenCctorAlreadyCallsDifferentMethod_DoesNotSkipPatch() {
        // Arrange
        TestHelper.PrepareAppServices();

        var csharpModule = TestHelper.CreateModuleDefinition(AssemblyCsharpDll);
        var modManagerInterfacesModule = TestHelper.CreateModuleDefinition(ModManagerInterfaces);
        var modManagerModule = TestHelper.CreateModuleDefinition(ModManager);

        var logManagerType = new TypeDefinition("Logging", "LogManager", TypeAttributes.Class);
        var modManagerType = new TypeDefinition("Railroader.ModManager", "ModManager", TypeAttributes.Class);
        var bootstrapMethod = new MethodDefinition("Bootstrap", MethodAttributes.Public | MethodAttributes.Static, csharpModule.TypeSystem.Void);
        modManagerType.Methods.Add(bootstrapMethod);

        var cctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static, csharpModule.TypeSystem.Void);
        var il = cctor.Body.GetILProcessor();
        var importedOther = new MethodReference("Other", csharpModule.TypeSystem.Void, modManagerType) { HasThis = false };
        il.Emit(OpCodes.Call, importedOther);
        il.Emit(OpCodes.Ret);
        logManagerType.Methods.Add(cctor);

        var importedBootstrap = new MethodReference("Bootstrap", csharpModule.TypeSystem.Void, modManagerType) { HasThis = false };
        TestHelper.StaticCtorCall(logManagerType);

        var assemblyPath = Path.Combine(TestHelper.ManagedPath, AssemblyCsharpDll + ".dll");
        var backupPath = assemblyPath.Replace(".dll", "_original.dll");

        Action[] calls = [
            TestHelper.GetCurrentDirectoryCall,
            TestHelper.FileExistsCall(ModManagerInterfaces, true),
            TestHelper.FileExistsCall(ModManager, true),
            TestHelper.FileExistsCall(AssemblyCsharpDll, true),
            TestHelper.ReadModuleCall(AssemblyCsharpDll, csharpModule),
            TestHelper.ReadModuleCall(ModManagerInterfaces, modManagerInterfacesModule),
            TestHelper.ReadModuleCall(ModManager, modManagerModule),
            TestHelper.GetTypeCall(csharpModule, "Logging.LogManager", logManagerType),
            TestHelper.GetTypeCall(modManagerModule, ModManagerType, modManagerType),
            TestHelper.FindBootstrapMethod(modManagerType),
            TestHelper.ImportReferenceCall(csharpModule, importedBootstrap),

            () => _ = cctor.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand == importedBootstrap),
            () => AppServices.File.Copy(assemblyPath, backupPath),
            () => csharpModule.Write(assemblyPath),
            () => AppServices.File.SetLastWriteTime(backupPath, Arg.Any<DateTime>()),
            () => AppServices.Console.WriteLine("Successfully patched game.")
        ];

        // Act
        Patcher.PatchGame();

        // Assert
        TestHelper.VerifyReceivedCalls(calls);

        cctor.Body.Instructions.Count.ShouldBe(3);
        cctor.Body.Instructions[0]!.OpCode.ShouldBe(OpCodes.Call);
        cctor.Body.Instructions[0]!.Operand.ShouldBe(importedBootstrap);
        cctor.Body.Instructions[1]!.OpCode.ShouldBe(OpCodes.Call);
        cctor.Body.Instructions[1]!.Operand.ShouldBe(importedOther);
        cctor.Body.Instructions[2]!.OpCode.ShouldBe(OpCodes.Ret);
    }
}
