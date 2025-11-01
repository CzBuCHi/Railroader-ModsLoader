//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.IO;
//using System.Reflection;
//using System.Text;
//using MemoryFileSystem;
//using Mono.Cecil;
//using NSubstitute;
//using Railroader.ModManager.CodePatchers.Special;
//using Railroader.ModManager.Delegates.Mono.Cecil;
//using Railroader.ModManager.Delegates.Mono.CSharp.CompilerCallableEntryPoint;
//using Railroader.ModManager.Delegates.System.IO.Directory;
//using Railroader.ModManager.Delegates.System.Reflection.Assembly;
//using Railroader.ModManager.Extensions;
//using Railroader.ModManager.Interfaces;
//using Railroader.ModManager.Services;
//using Railroader.ModManager.Services.Factories;
//using Railroader.ModManager.Services.Wrappers;
//using Railroader.ModManager.Services.Wrappers.FileSystem;
//using Serilog;

//namespace Railroader.ModManager.Tests;

//[ExcludeFromCodeCoverage]
//public sealed class TestServiceManager
//{
//    public IServiceProvider ServiceProvider { get; }
//    public MemoryFs         MemoryFs        { get; }

//    private readonly Dictionary<Type, object>     _Services   = new();
//    private readonly Dictionary<string, Assembly> _Assemblies = new();

//    public TestServiceManager(string? currentDirectory = null) {
//        MemoryFs = new MemoryFs(currentDirectory);

//        ServiceProvider = Substitute.For<IServiceProvider>();
//        ServiceProvider.GetService(Arg.Any<Type>()).Returns(o => GetService(o.Arg<Type>()));

//        ModManager.ServiceProvider = ServiceProvider;

//        MainLogger = MockLogger();
//        LoggerFactory = MockLoggerFactory();
//        LoadAssemblyFrom = MockLoadAssemblyFromDelegate();

//        _Services.Add(typeof(IFileSystem), MemoryFs.FileSystem);
//        _Services.Add(typeof(ILogger), MainLogger);
//        _Services.Add(typeof(ILoggerFactory), LoggerFactory);
//        _Services.Add(typeof(LoadFrom), LoadAssemblyFrom);
//        _Services.Add(typeof(IHarmonyFactory), MockHarmonyFactory());
//        _Services.Add(typeof(ReadAssemblyDefinition), ReadAssemblyDefinition);
//        _Services.Add(typeof(WriteAssemblyDefinition), WriteAssemblyDefinition);
//        _Services.Add(typeof(IModdingContext), ModdingContext);
//        _Services.Add(typeof(IMemoryLogger), MemoryLogger);
//    }

//    private object GetService(Type type) => _Services.TryGetValue(type, out var service) ? service : throw new InvalidOperationException($"Mock not registered: {type}");

//    public TService GetService<TService>() => (TService)GetService(typeof(TService));

//    // MOCKS
//    public ILogger                         MainLogger              { get; }
//    public ILogger                         ContextLogger           { get; } = Substitute.For<ILogger>();
//    public ILoggerFactory                  LoggerFactory           { get; }
//    public LoadFrom                LoadAssemblyFrom        { get; }
//    public IHarmony                 Harmony          { get; } = Substitute.For<IHarmony>();
//    public ReadAssemblyDefinition  ReadAssemblyDefinition  { get; } = Substitute.For<ReadAssemblyDefinition>();
//    public WriteAssemblyDefinition WriteAssemblyDefinition { get; } = Substitute.For<WriteAssemblyDefinition>();
//    public IModdingContext                 ModdingContext          { get; } = Substitute.For<IModdingContext>();
//    public IMemoryLogger                   MemoryLogger            { get; } = Substitute.For<IMemoryLogger>();

//    // MOCK factories

//    private ILogger MockLogger() {
//        var logger = Substitute.For<ILogger>();
//        logger.ForContext("SourceContext", Arg.Any<string>()).Returns(_ => ContextLogger);
//        return logger;
//    }

//    private ILoggerFactory MockLoggerFactory() {
//        var loggerFactory = Substitute.For<ILoggerFactory>();
//        loggerFactory.GetLogger(Arg.Any<string>()).Returns(_ => ContextLogger);
//        return loggerFactory;
//    }

//    private IHarmonyFactory MockHarmonyFactory() {
//        var harmonyFactory = Substitute.For<IHarmonyFactory>();
//        harmonyFactory.CreateHarmony(Arg.Any<string>()).Returns(_ => Harmony);
//        return harmonyFactory;
//    }

//    private LoadFrom MockLoadAssemblyFromDelegate() {
//        var loadAssemblyFrom = Substitute.For<LoadFrom>();
//        loadAssemblyFrom.Invoke(Arg.Any<string>()).Returns(o => {
//            var assemblyFile = o.Arg<string>();
//            _Assemblies.TryGetValue(assemblyFile, out var assembly);
//            return assembly;
//        });
//        return loadAssemblyFrom;
//    }

//    // SUT factories
//    public PluginManagerFactory CreatePluginManagerFactory() =>
//        new(
//            ServiceProvider.GetService<LoadFrom>(),
//            ServiceProvider.GetService<ILogger>()
//        );

//    public PluginManager CreatePluginManager() =>
//        new(
//            ServiceProvider.GetService<LoadFrom>(),
//            ServiceProvider.GetService<IModdingContext>(),
//            ServiceProvider.GetService<ILogger>()
//        );

//    public ModExtractor CreateModExtractor() =>
//        new(
//            ServiceProvider.GetService<IFileSystem>(),
//            ServiceProvider.GetService<IMemoryLogger>(),
//            ServiceProvider.GetService<GetCurrentDirectory>()
//        );

//    public ModDefinitionProcessor CreateModDefinitionProcessor() =>
//        new(
//            ServiceProvider.GetService<ILogger>()
//        );

//    public ModDefinitionLoader CreateModDefinitionLoader() =>
//        new(
//            ServiceProvider.GetService<IFileSystem>(),
//            ServiceProvider.GetService<IMemoryLogger>(),
//            ServiceProvider.GetService<GetCurrentDirectory>(),
//            ServiceProvider.GetService<EnumerateDirectories>()
//        );

//    public LoggerFactory CreateLoggerFactory() =>
//        new(
//            ServiceProvider.GetService<ILogger>()
//        );

//    public CodeCompiler CreateCodeCompiler(string[]? referenceNames = null) {
//        var fileSystem          = ServiceProvider.GetService<IFileSystem>();
//        var assemblyCompiler    = ServiceProvider.GetService<CompileAssemblyDelegate>();
//        var service             = ServiceProvider.GetService<ILogger>();
//        var getCurrentDirectory = ServiceProvider.GetService<GetCurrentDirectory>();

//        return referenceNames == null
//            ? new CodeCompiler(fileSystem, assemblyCompiler, service, getCurrentDirectory)
//            : new CodeCompiler(fileSystem, assemblyCompiler, service, getCurrentDirectory) { ReferenceNames = referenceNames };
//    }

//    public CodePatcher CreateCodePatcher(List<(Type InterfaceType, Type PluginPatcherType)>? pluginPatchers = null) {
//        var fileSystem              = ServiceProvider.GetService<IFileSystem>();
//        var readAssemblyDefinition  = ServiceProvider.GetService<ReadAssemblyDefinition>();
//        var writeAssemblyDefinition = ServiceProvider.GetService<WriteAssemblyDefinition>();
//        var service                 = ServiceProvider.GetService<ILogger>();
//        var getCurrentDirectory     = ServiceProvider.GetService<GetCurrentDirectory>();
//        var enumerateDirectories    = ServiceProvider.GetService<EnumerateDirectories>();

//        return pluginPatchers == null
//            ? new CodePatcher(fileSystem, readAssemblyDefinition,writeAssemblyDefinition, service, getCurrentDirectory, enumerateDirectories)
//            : new CodePatcher(fileSystem, readAssemblyDefinition, writeAssemblyDefinition,service, getCurrentDirectory, enumerateDirectories) { PluginPatchers = pluginPatchers };
//    }
    
//    public HarmonyPluginPatcher CreateHarmonyPluginPatcher() =>
//        new(
//            ServiceProvider.GetService<ILoggerFactory>()
//        );

//    public TopRightButtonPluginPatcher CreateTopRightButtonPluginPatcher() =>
//        new(
//            ServiceProvider.GetService<ILoggerFactory>()
//        );

//    public CompileAssemblyDelegate CreateCompileAssembly() {
//        return (string outputPath, ICollection<string> sources, ICollection<string> references, out string messages) =>
//            CompileAssemblyCore.CompileAssembly(ServiceProvider.GetService<InvokeCompiler>(), ServiceProvider.GetService<ILoggerFactory>().GetLogger())
//                (outputPath, sources, references, out messages);
//    }

//    // Extensions
//    public TestServiceManager WithFile(string path, string content, DateTime? lastWriteTime = null) {
//        MemoryFs.Add(path, content, lastWriteTime);
//        return this;
//    }

//    public TestServiceManager WithAssemblyCompiler(Action<CompileAssemblyDelegate>? configure = null) {
//        var assemblyCompiler = Substitute.For<CompileAssemblyDelegate>();
//        configure?.Invoke(assemblyCompiler);
//        _Services.Add(typeof(CompileAssemblyDelegate), assemblyCompiler);
//        return this;
//    }

//    public TestServiceManager WithAssemblyDefinition(AssemblyDefinition assemblyDefinition) {
//        ReadAssemblyDefinition.Invoke(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);
//        WriteAssemblyDefinition.When(o => o.Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>())).Do(o => {
//            var       definition   = o.Arg<AssemblyDefinition>();
//            var       fileName     = o.Arg<string>();
//            using var stream       = MemoryFs.FileSystem.File.Create(fileName);
//            using var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true);
//            streamWriter.Write(definition.Name.Name);
//        });

//        return this;
//    }

//    public TestServiceManager WithCompilerCallableEntryPoint(bool compilerResult, string compilerMessages) {
//        var compilerCallableEntryPoint = Substitute.For<InvokeCompiler>();
//        compilerCallableEntryPoint.Invoke(Arg.Any<string[]>(), Arg.Is<TextWriter>(_ => true)).Returns(o => {
//            var writer = o.ArgAt<TextWriter>(1);
//            writer.Write(compilerMessages);
//            return compilerResult;
//        });
//        _Services.Add(typeof(InvokeCompiler), compilerCallableEntryPoint);
//        return this;
//    }

//    public TestServiceManager WithAssembly(string path, string source) {
//        _Assemblies.Add(path, TestUtils.BuildAssembly(source, [typeof(TestServiceManager).Assembly.GetName().Name]));
//        return this;
//    }
//}
