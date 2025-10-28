using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Railroader.ModManagerInstaller;

public static class Patcher
{
    private const string ModManagerInterfaces = "Railroader.ModManager.Interfaces";
    private const string ModManager           = "Railroader.ModManager";
    private const string ModManagerType       = "Railroader.ModManager.ModManager";
    private const string AssemblyCsharpDll    = "Assembly-CSharp.dll";

    public static void PatchGame() {
        var path           = Path.Combine(Environment.CurrentDirectory, "Railroader_Data", "Managed");
        var modInterfaces  = GetFile(path, ModManagerInterfaces + ".dll");
        var modInjector    = GetFile(path, ModManager + ".dll");
        var assemblyCsharp = GetFile(path, AssemblyCsharpDll);

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(path);

        var readerParameters = new ReaderParameters {
            InMemory = true,
            AssemblyResolver = resolver
        };

        var assemblyCsharpModule = ModuleDefinition.ReadModule(assemblyCsharp, readerParameters)!;

        if (assemblyCsharpModule.AssemblyReferences.Any(o => o.Name == ModManager)) {
            ConsoleEx.WriteWarning("Railroader is already patched.");
            return;
        }

        InjectModule(assemblyCsharpModule, ModManagerInterfaces, modInterfaces, readerParameters);
        var modManager = InjectModule(assemblyCsharpModule, ModManager, modInjector, readerParameters)!;

        PatchLogManager(assemblyCsharpModule, modManager);

        File.Copy(assemblyCsharp, assemblyCsharp.Replace(".dll", "_original.dll"));
        assemblyCsharpModule.Write(assemblyCsharp);
        File.SetLastWriteTime(assemblyCsharp.Replace(".dll", "_original.dll"), File.GetLastWriteTime(assemblyCsharp));
    }

    private static void PatchLogManager(ModuleDefinition assemblyCsharp, ModuleDefinition modManager) {
        // Use Mono.Cecil to create static constructor on Logging.LogManager type that calls Railroader.ModManager.ModManager.Bootstrap()

        var logManager = assemblyCsharp.GetType("Logging.LogManager");
        if (logManager == null) {
            ConsoleEx.WriteFatal("Could not find Logging.LogManager type.");
            return;
        }

        var modManagerType = modManager.GetType(ModManagerType);
        if (modManagerType == null) {
            ConsoleEx.WriteFatal($"Could not find {ModManagerType} type.");
            return;
        }

        var bootstrapMethod = modManagerType.Methods.FirstOrDefault(m => m.Name == "Bootstrap");
        if (bootstrapMethod == null) {
            ConsoleEx.WriteFatal($"Could not find {ModManagerType}.Bootstrap method.");
            return;
        }

        // Import Bootstrap method
        var bootstrapMethodRef = assemblyCsharp.ImportReference(bootstrapMethod);

        var cctor = logManager.Methods.FirstOrDefault(m => m.Name == ".cctor");
        if (cctor != null) {
            // Check if Bootstrap is already called
            if (cctor.Body.Instructions.Any(o => o.OpCode == OpCodes.Call && o.Operand == bootstrapMethodRef)) {
                ConsoleEx.WriteWarning("Logging.LogManager static constructor already calls ModManager.Bootstrap. Skipping patch.");
                return;
            }
        } else {
            // Create new empty cctor
            cctor = new MethodDefinition(
                ".cctor",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                assemblyCsharp.TypeSystem.Void
            );
            logManager.Methods.Add(cctor);

            // Initialize with a ret instruction
            var il  = cctor.Body.GetILProcessor();
            var ret = il.Create(OpCodes.Ret);
            il.Append(ret);

            cctor.Body.InitLocals = true;
            cctor.Body.MaxStackSize = 8;
        }

        // Append Bootstrap call to the end
        var ilProcessor     = cctor.Body.GetILProcessor();
        var lastInstruction = cctor.Body.Instructions.First(); 
        var callBootstrap   = ilProcessor.Create(OpCodes.Call, bootstrapMethodRef);

        // Insert Bootstrap call before first instruction
        ilProcessor.InsertBefore(lastInstruction, callBootstrap);
        
        Console.WriteLine("Successfully patched game.");
    }

    private static ModuleDefinition? InjectModule(ModuleDefinition assemblyCsharpModule, string name, string path, ReaderParameters readerParameters) {
        var modInterfacesModule = ModuleDefinition.ReadModule(path, readerParameters);
        if (modInterfacesModule == null) {
            ConsoleEx.WriteFatal($"Could not load module '{name}'.");
            return null;
        }

        var modInterfacesReference = new AssemblyNameReference(name, modInterfacesModule.Assembly.Name.Version);
        assemblyCsharpModule.AssemblyReferences.Add(modInterfacesReference);
        return modInterfacesModule;
    }

    private static string GetFile(string path, string name) {
        var filePath = Path.Combine(path, name);
        if (!File.Exists(filePath)) {
            ConsoleEx.WriteFatal($"Could not locate file '{name}'.");
        }

        return filePath;
    }
}
