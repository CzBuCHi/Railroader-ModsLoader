using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Railroader.ModManagerInstaller.Abstractions;

namespace Railroader.ModManagerInstaller;

[PublicAPI]
public static class Patcher
{
    private const string ModManagerInterfaces = "Railroader.ModManager.Interfaces";
    private const string ModManager = "Railroader.ModManager";
    private const string ModManagerType = "Railroader.ModManager.ModManager";
    private const string AssemblyCsharpDll = "Assembly-CSharp.dll";

    public static Action PatchGame = PatchGameCore;
    
    public static void PatchGameCore() {

        var path = Path.Combine(AppServices.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed");
        var modInterfaces = GetFile(path, ModManagerInterfaces + ".dll");
        var modInjector = GetFile(path, ModManager + ".dll");
        var assemblyCsharp = GetFile(path, AssemblyCsharpDll);

        var resolver = new DefaultAssemblyResolver();
        resolver.RemoveSearchDirectory(".");
        resolver.RemoveSearchDirectory("bin");
        resolver.AddSearchDirectory(path);

        var readerParameters = new ReaderParameters {
            InMemory = true,
            AssemblyResolver = resolver
        };
        
        var assemblyCsharpModule = AppServices.ModuleDefinition.ReadModule(assemblyCsharp, readerParameters);
        if (assemblyCsharpModule.AssemblyReferences.Any(o => o.Name == ModManager)) {
            AppServices.Console.WriteLine("Railroader is already patched.", ConsoleColor.DarkYellow);
            return;
        }

        InjectModule(assemblyCsharpModule, ModManagerInterfaces, modInterfaces, readerParameters);
        var modManager = InjectModule(assemblyCsharpModule, ModManager, modInjector, readerParameters);

        var hasPatch = PatchLogManager(assemblyCsharpModule, modManager);
        if (!hasPatch) {
            return;
        }

        AppServices.File.Copy(assemblyCsharp, assemblyCsharp.Replace(".dll", "_original.dll"));
        assemblyCsharpModule.Write(assemblyCsharp);
        AppServices.File.SetLastWriteTime(assemblyCsharp.Replace(".dll", "_original.dll"), AppServices.File.GetLastWriteTime(assemblyCsharp));
        AppServices.Console.WriteLine("Successfully patched game.");
    }

    private static bool PatchLogManager(IModuleDefinition assemblyCsharp, IModuleDefinition modManager) {
        // Use Mono.Cecil to create static constructor on Logging.LogManager type that calls Railroader.ModManager.ModManager.Bootstrap()

        var logManager = assemblyCsharp.GetType("Logging.LogManager");
        if (logManager == null) {
            throw new InstallerException("Could not find Logging.LogManager type.");
        }

        var modManagerType = modManager.GetType(ModManagerType);
        if (modManagerType == null) {
            throw new InstallerException($"Could not find {ModManagerType} type.");
        }

        var bootstrapMethod = modManagerType.Methods.FirstOrDefault(m => m.Name == "Bootstrap");
        if (bootstrapMethod == null) {
            throw new InstallerException($"Could not find {ModManagerType}.Bootstrap method.");
        }

        // Import Bootstrap method
        var bootstrapMethodRef = assemblyCsharp.ImportReference(bootstrapMethod);

        var cctor = logManager.Methods.FirstOrDefault(m => m.Name == ".cctor");
        if (cctor != null) {
            // Check if Bootstrap is already called
            if (cctor.Body.Instructions.Any(o => o.OpCode == OpCodes.Call && o.Operand == bootstrapMethodRef)) {
                AppServices.Console.WriteLine("Logging.LogManager static constructor already calls ModManager.Bootstrap. Skipping patch.",
                    ConsoleColor.DarkYellow);
                return false;
            }
        } else {
            // Create new empty cctor
            const MethodAttributes cctorAttributes = MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig |
                                                     MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            cctor = new(".cctor", cctorAttributes, assemblyCsharp.TypeSystem.Void);
            logManager.Methods.Add(cctor);

            var il = cctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ret);
        }

        // Append Bootstrap call to the end
        var ilProcessor = cctor.Body.GetILProcessor();
        var firstInstruction = cctor.Body.Instructions[0]!;
        var callBootstrap = ilProcessor.Create(OpCodes.Call, bootstrapMethodRef);

        // Insert Bootstrap call before first instruction
        ilProcessor.InsertBefore(firstInstruction, callBootstrap);
        return true;
    }

    private static IModuleDefinition InjectModule(IModuleDefinition assemblyCsharpModule, string name, string path, ReaderParameters readerParameters) {
        var modInterfacesModule = AppServices.ModuleDefinition.ReadModule(path, readerParameters);
        var modInterfacesReference = new AssemblyNameReference(name, modInterfacesModule.Assembly.Name.Version);
        assemblyCsharpModule.AssemblyReferences.Add(modInterfacesReference);
        return modInterfacesModule;
    }

    private static string GetFile(string path, string name) {
        var filePath = Path.Combine(path, name);
        if (!AppServices.File.Exists(filePath)) {
            throw new InstallerException($"Could not locate file '{name}'.");
        }

        return filePath;
    }
}