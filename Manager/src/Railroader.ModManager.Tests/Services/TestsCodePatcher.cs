//using System;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using FluentAssertions;
//using Mono.Cecil;
//using NSubstitute;
//using Railroader.ModManager.CodePatchers;
//using Railroader.ModManager.Interfaces;
//using Serilog;

//namespace Railroader.ModManager.Tests.Services;

//public sealed class TestsCodePatcher
//{
//    private static readonly DateTime _OldDate = new(2000, 1, 2);
//    private static readonly DateTime _NewDate = new(2000, 1, 4);

//    private const string AssemblyPath = @"C:\Current\Mods\DummyMod\DummyMod.dll";

//    private static readonly ModDefinition _ModDefinition = new() {
//        Identifier = "DummyMod",
//        Name = "Dummy Mod Name",
//        BasePath = @"C:\Current\Mods\DummyMod"
//    };


//    [Fact]
//    public void CompileMod_Compilation_WithPatches_AssemblyLoadFail() {
//        // Arrange
//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source.cs", "", _NewDate);

//        serviceManager.WithAssemblyCompiler(assemblyCompiler => {
//            assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
//                            .Returns(_ => true)
//                            .AndDoes(o => serviceManager.MemoryFs.Add(o.ArgAt<string>(0), "Compiled DLL"));
//        });

//        var sut = serviceManager.CreateCodePatcher([(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))]);

//        // Act
//        sut.ApplyPatches(_ModDefinition);

//        // Assert
//        serviceManager.MainLogger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", AssemblyPath, _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", AssemblyPath, _ModDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(3);
//    }

//    [Fact]
//    public void CompileMod_Compilation_WithPatches_ReturnValidInstances() {
//        // Arrange
//        const string source = """
//                              using Railroader.ModManager.Interfaces;
//                              using Serilog;

//                              namespace Foo.Bar
//                              {
//                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
//                                  {
//                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
//                                          : base(moddingContext, mod) {
//                                      }
//                                  }
//                              }
//                              """;

//        var (assemblyDefinition, _) = TestUtils.BuildAssemblyDefinition(source);

//        string[] expectedDirectories = [
//            ".",
//            "bin",
//            @"C:\Current\Railroader_Data\Managed",
//            @"C:\Current\Mods\SecondMod"
//        ];

//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source.cs", "", _NewDate)
//                .WithFile(@"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate)
//                .WithAssemblyDefinition(assemblyDefinition);

//        serviceManager.WithAssembly(AssemblyPath, source);
        
//        var sut = serviceManager.CreateCodePatcher([(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))]);

//        // Act
//        sut.ApplyPatches(_ModDefinition);

//        // Assert
//        serviceManager.MainLogger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Debug("TestPluginPatcher::Patch | typeDefinition: {type}", "FirstPlugin");
//        serviceManager.MainLogger.Received().Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", Arg.Any<string>(), _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(4);

//        serviceManager.ReadAssemblyDefinition.Received(1).Invoke(AssemblyPath,
//            Arg.Is<ReaderParameters>(o =>
//                o.AssemblyResolver is DefaultAssemblyResolver &&
//                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
//            )
//        );
//        serviceManager.WriteAssemblyDefinition.Received(1).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());

//        serviceManager.MemoryFs.FileSystem.File.Received(1).Delete(AssemblyPath);
//        serviceManager.MemoryFs.FileSystem.File.Received().Move(@"C:\Current\Mods\DummyMod\DummyMod.patched.dll", AssemblyPath);

//        // verify assemblyDefinition.Dispose as called ...
//        var imageField  = typeof(ModuleDefinition).GetField("Image", BindingFlags.Instance | BindingFlags.NonPublic)!;
//        var image       = imageField.GetValue(assemblyDefinition.MainModule)!; // Mono.Cecil.PE.Image
//        var streamField = image.GetType().GetField("Stream", BindingFlags.Instance | BindingFlags.Public)!;
//        var disposable  = streamField.GetValue(image)!; // Mono.Disposable<System.IO.Stream>
//        var valueField  = disposable.GetType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)!;
//        var stream      = (Stream)valueField.GetValue(disposable)!;
//        stream.CanRead.Should().BeFalse();
//        stream.CanWrite.Should().BeFalse();
//    }

//    [Fact]
//    public void CompileMod_Compilation_WithPatches_ExtraInterface() {
//        // Arrange
//        const string source = """
//                              using Railroader.ModManager.Interfaces;
//                              using Serilog;

//                              namespace Foo.Bar
//                              {
//                                  public interface IExtra {}
                                  
//                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IExtra
//                                  {
//                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
//                                          : base(moddingContext, mod) {
//                                      }
//                                  }
//                              }
//                              """;

//        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);

//        string[] expectedDirectories = [
//            ".",
//            "bin",
//            @"C:\Current\Railroader_Data\Managed",
//            @"C:\Current\Mods\SecondMod"
//        ];

//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source.cs", "", _NewDate)
//                .WithFile(@"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate)
//                .WithAssemblyDefinition(assemblyDefinition);

//        serviceManager.WithAssembly(AssemblyPath, source);


//        var sut = serviceManager.CreateCodePatcher([(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))]);

//        // Act
//        sut.ApplyPatches(_ModDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "patched");

//        // Assert
//        serviceManager.MainLogger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(3);

//        serviceManager.ReadAssemblyDefinition.Received(1).Invoke(AssemblyPath,
//            Arg.Is<ReaderParameters>(o =>
//                o.AssemblyResolver is DefaultAssemblyResolver &&
//                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
//            )
//        );
//        serviceManager.WriteAssemblyDefinition.Received(0).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
//    }

//    [Fact]
//    public void CompileMod_Compilation_WithPatches_HandleThrowingPatcher1() {
//        // Arrange
   
//        const string source = """
//                              using Railroader.ModManager.Interfaces;
//                              using Serilog;

//                              namespace Foo.Bar
//                              {
//                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
//                                  {
//                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
//                                          : base(moddingContext, mod) {
//                                      }
//                                  }
//                              }
//                              """;

//        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);

//        string[] expectedDirectories = [
//            ".",
//            "bin",
//            @"C:\Current\Railroader_Data\Managed",
//            @"C:\Current\Mods\SecondMod"
//        ];
//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source.cs", "", _NewDate)
//                .WithFile(@"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate)
//                .WithAssemblyDefinition(assemblyDefinition);


//        serviceManager.WithAssembly(AssemblyPath, source);

//        var sut = serviceManager.CreateCodePatcher([(typeof(IHarmonyPlugin), typeof(ThrowingPatcher))]);

//        // Act
//        sut.ApplyPatches(_ModDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "patched");

//        // Assert
//        serviceManager.MainLogger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Error(Arg.Is<Exception>(o => o.Message == "ThrowingPatcher"), "Failed to patch type {TypeName} for mod {ModId}", "Foo.Bar.FirstPlugin", _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(4);

//        serviceManager.ReadAssemblyDefinition.Received(1).Invoke(AssemblyPath,
//            Arg.Is<ReaderParameters>(o =>
//                o.AssemblyResolver is DefaultAssemblyResolver &&
//                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
//            )
//        );
//        serviceManager.WriteAssemblyDefinition.Received(0).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
//    }

//    [Fact]
//    public void CompileMod_Compilation_WithPatches_HandleThrowingPatcher2() {
//        // Arrange
        
//        const string source = """
//                              using System;
//                              using Railroader.ModManager.Interfaces;
//                              using Railroader.ModManager.Tests.Services;
//                              using Serilog;

//                              namespace Foo.Bar
//                              {
//                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin, ITopRightButtonPlugin
//                                  {
//                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
//                                          : base(moddingContext, mod) {
//                                      }
                                      
//                                      public string IconName => "";
//                                      public string Tooltip => "";
//                                      public int Index => 0;
//                                      public Action OnClick => () => {};
//                                  }
//                              }
//                              """;

//        var (assemblyDefinition, _) = TestUtils.BuildAssemblyDefinition(source);

//        string[] expectedDirectories = [
//            ".",
//            "bin",
//            @"C:\Current\Railroader_Data\Managed",
//            @"C:\Current\Mods\SecondMod"
//        ];

//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source.cs", "", _NewDate)
//                .WithFile(@"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate)
//                .WithAssemblyDefinition(assemblyDefinition);

//        serviceManager.WithAssembly(AssemblyPath, source);

//        var sut = serviceManager.CreateCodePatcher([
//            (typeof(IHarmonyPlugin), typeof(TestPluginPatcher)),
//            (typeof(ITopRightButtonPlugin), typeof(ThrowingPatcher))
//        ]);


//        // Act
//        sut.ApplyPatches(_ModDefinition);

//        // Assert
//        serviceManager.MainLogger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Debug("TestPluginPatcher::Patch | typeDefinition: {type}", "FirstPlugin");
//        serviceManager.MainLogger.Received().Error(Arg.Any<Exception>(), "Failed to patch type {TypeName} for mod {ModId}", "Foo.Bar.FirstPlugin", _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
//        serviceManager.MainLogger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(5);

//        serviceManager.ReadAssemblyDefinition.Received(1).Invoke(AssemblyPath,
//            Arg.Is<ReaderParameters>(o =>
//                o.AssemblyResolver is DefaultAssemblyResolver &&
//                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
//            )
//        );
//        serviceManager.WriteAssemblyDefinition.Received(0).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
//    }
    
//    private sealed class TestPluginPatcher(ILogger logger) : ITypePatcher
//    {
//        public bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) {
//            logger.Debug("TestPluginPatcher::Patch | typeDefinition: {type}", typeDefinition.Name);
//            return true;
//        }
//    }

//#pragma warning disable CS9113 // Parameter is unread.
//    private sealed class ThrowingPatcher(ILogger logger) : ITypePatcher
//#pragma warning restore CS9113 // Parameter is unread.
//    {
//        public bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) => throw new Exception("ThrowingPatcher");
//    }
//}
