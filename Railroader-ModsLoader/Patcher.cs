using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Railroader.ModsLoader;

internal static class Patcher
{
    private const string ModInterfaces     = "Railroader-ModInterfaces";
    private const string ModInjector       = "Railroader-ModInjector";
    private const string AssemblyCsharpDll = "Assembly-CSharp.dll";

    public static bool PatchGame() {
        var path           = Path.Combine(Environment.CurrentDirectory, "Railroader_Data", "Managed");
        var modInterfaces  = GetFile(path, ModInterfaces + ".dll");
        var modInjector    = GetFile(path, ModInjector + ".dll");
        var assemblyCsharp = GetFile(path, AssemblyCsharpDll);

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(path);

        var readerParameters = new ReaderParameters {
            InMemory = true,
            AssemblyResolver = resolver
        };

        var assemblyCsharpModule = ModuleDefinition.ReadModule(assemblyCsharp, readerParameters)!;

        if (assemblyCsharpModule.AssemblyReferences!.Any(o => o.Name == ModInjector)) {
            Program.WriteWarning("Railroader is already patched.");
            return false;
        }

        InjectModule(assemblyCsharpModule, ModInterfaces, modInterfaces, readerParameters);
        var modInjectorModule = InjectModule(assemblyCsharpModule, ModInjector, modInjector, readerParameters);

        PatchLogManagerAwake(assemblyCsharpModule, modInjectorModule);

        File.Copy(assemblyCsharp, assemblyCsharp.Replace(".dll", "_original.dll"));
        assemblyCsharpModule.Write(assemblyCsharp);
        File.SetLastWriteTime(assemblyCsharp.Replace(".dll", "_original.dll"), File.GetLastWriteTime(assemblyCsharp)); // todo: remove
        return true;
    }

    private static ModuleDefinition InjectModule(ModuleDefinition assemblyCsharpModule, string name, string path, ReaderParameters readerParameters) {
        var modInterfacesModule = ModuleDefinition.ReadModule(path, readerParameters);
        if (modInterfacesModule == null) {
            Program.WriteFatal($"Could not load module '{name}'.");
            return null!;
        }

        var modInterfacesReference = new AssemblyNameReference(name, modInterfacesModule.Assembly!.Name!.Version!);
        assemblyCsharpModule.AssemblyReferences!.Add(modInterfacesReference);
        return modInterfacesModule;
    }

    private static void PatchLogManagerAwake(ModuleDefinition assemblyCsharp, ModuleDefinition modInjector) {
        /*           
            original Awake method:
            
            private void Awake()
            {
                Log.Logger = MakeConfiguration()
                            .MinimumLevel.Information()
                            .MinimumLevel.Override("Model.AI.AutoEngineer", LogEventLevel.Warning)
                            .MinimumLevel.Override("Model.AI.AutoEngineerPlanner", LogEventLevel.Warning)
                            .MinimumLevel.Override("Effects.Decals.CanvasDecalRenderer", LogEventLevel.Warning)
                            .CreateLogger();
                Log.Information("Railroader {appVersion} ({buildId})", Application.version, App.Client.BuildId);
            }
            
            patched Awake method:
            
            private void Awake()
            {
                Log.Logger =

                    Railroader.ModInjector.Injector.CreateLogger(

                        MakeConfiguration()
                            .MinimumLevel.Information()
                            .MinimumLevel.Override("Model.AI.AutoEngineer", LogEventLevel.Warning)
                            .MinimumLevel.Override("Model.AI.AutoEngineerPlanner", LogEventLevel.Warning)
                            .MinimumLevel.Override("Effects.Decals.CanvasDecalRenderer", LogEventLevel.Warning)

                    );

                Log.Information("Railroader {appVersion} ({buildId})", Application.version, App.Client.BuildId);

                Railroader.ModInjector.Injector.ModInjectorMain();
            }
         */

        var logManager = assemblyCsharp.GetType("Logging.LogManager");
        if (logManager == null) {
            Program.WriteFatal("Could not find Logging.LogManager type.");
            return;
        }

        var awake = logManager.Methods!.FirstOrDefault(o => o.Name == "Awake");
        if (awake == null) {
            Program.WriteFatal("Could not find Awake method in Logging.LogManager.");
            return;
        }

        var createLogger = awake.Body!.Instructions!.FirstOrDefault(o =>
            o.OpCode == OpCodes.Callvirt &&
            o.Operand is MethodReference { Name: "CreateLogger" } method &&
            method.DeclaringType!.FullName == "Serilog.LoggerConfiguration");

        if (createLogger == null) {
            Program.WriteFatal("Could not find CreateLogger instruction in Awake method.");
            return;
        }

        var injectorType           = modInjector.GetType("Railroader.ModInjector.Injector")!;

        var modInjectorMain         = injectorType.Methods!.FirstOrDefault(o => o.Name == "ModInjectorMain")!;
        var importedModInjectorMain = assemblyCsharp.ImportReference(modInjectorMain)!;

        var createLoggerEx          = injectorType.Methods!.FirstOrDefault(o => o.Name == "CreateLogger")!;
        var importedCreateLoggerEx  = assemblyCsharp.ImportReference(createLoggerEx)!;

        var ilProcessor = awake.Body.GetILProcessor()!;
        var returnInstruction = awake.Body.Instructions!.Last(i => i.OpCode == OpCodes.Ret);

        ilProcessor.InsertBefore(returnInstruction, ilProcessor.Create(OpCodes.Call, importedModInjectorMain)!);
        ilProcessor.Replace(createLogger, ilProcessor.Create(OpCodes.Call, importedCreateLoggerEx)!);
    }

    private static string GetFile(string path, string name) {
        var filePath = Path.Combine(path, name);
        if (!File.Exists(filePath)) {
            Program.WriteFatal($"Could not locate file '{name}'.");
        }

        return filePath;
    }
}
