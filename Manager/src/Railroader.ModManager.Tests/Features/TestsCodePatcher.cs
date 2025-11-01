using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MemoryFileSystem;
using Mono.Cecil;
using NSubstitute;
using Railroader.ModManager.Delegates.Mono.Cecil;
using Railroader.ModManager.Features;
using Railroader.ModManager.Features.CodePatchers;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsCodePatcher
{
    private static readonly DateTime _OldDate = new(2000, 1, 2);
    private static readonly DateTime _NewDate = new(2000, 1, 4);

    private const string AssemblyPath = @"C:\Current\Mods\DummyMod\DummyMod.dll";

    private static readonly ModDefinition _ModDefinition = new()
    {
        Identifier = "DummyMod",
        Name = "Dummy Mod Name",
        BasePath = @"C:\Current\Mods\DummyMod"
    };

    private static ApplyPatchesDelegate Factory(ILogger logger, MemoryFs fileSystem, ReadAssemblyDefinition readAssemblyDefinition, WriteAssemblyDefinition writeAssemblyDefinition) =>
        CodePatcher.Factory(logger,
            readAssemblyDefinition,
            writeAssemblyDefinition,
            fileSystem.Directory.GetCurrentDirectory,
            fileSystem.Directory.EnumerateDirectories,
            fileSystem.File.Delete,
            fileSystem.File.Move
        );

    [Fact]
    public void NoPatches_DoNothing() {
        // Arrange
        var fileSystem = new MemoryFs(@"\Current") {
            { AssemblyPath, "", _OldDate },
            { @"C:\Current\Mods\DummyMod\source.cs", "", _NewDate }
        };

        var assemblyCompiler = Substitute.For<CompileAssemblyDelegate>();
        assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));

        var logger                  = Substitute.For<ILogger>();
        var readAssemblyDefinition  = Substitute.For<ReadAssemblyDefinition>();
        var writeAssemblyDefinition = Substitute.For<WriteAssemblyDefinition>();

        var applyPatches = Factory(logger, fileSystem, readAssemblyDefinition, writeAssemblyDefinition);

        // Act
        applyPatches(_ModDefinition);

        // Assert
        logger.ReceivedCalls().Should().HaveCount(0);
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_AssemblyLoadFail() {
        // Arrange
        var fileSystem = new MemoryFs(@"\Current") {
            { AssemblyPath, "", _OldDate },
            { @"C:\Current\Mods\DummyMod\source.cs", "", _NewDate }
        };

        var assemblyCompiler = Substitute.For<CompileAssemblyDelegate>();
        assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));

        var logger                  = Substitute.For<ILogger>();
        var readAssemblyDefinition  = Substitute.For<ReadAssemblyDefinition>();
        var writeAssemblyDefinition = Substitute.For<WriteAssemblyDefinition>();

        var applyPatches = Factory(logger, fileSystem, readAssemblyDefinition, writeAssemblyDefinition);

        // Act
        applyPatches(_ModDefinition, new TypePatcherInfo(typeof(IHarmonyPlugin), TestPluginPatcher.Factory));

        // Assert
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", AssemblyPath, _ModDefinition.Identifier);
        logger.Received().Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", AssemblyPath, _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(3);
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_ReturnValidInstances()
    {
        // Arrange
        const string source = """
                              using Railroader.ModManager.Interfaces;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                              }
                              """;

        var (assemblyDefinition, _) = TestUtils.BuildAssemblyDefinition(source);

        var fileSystem = new MemoryFs(@"\Current") {
            { AssemblyPath, "", _OldDate },
            { @"C:\Current\Mods\DummyMod\source.cs", "", _NewDate },
            { @"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate }
        };

        var assemblyCompiler = Substitute.For<CompileAssemblyDelegate>();
        assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));

        var logger                  = Substitute.For<ILogger>();
        var readAssemblyDefinition  = Substitute.For<ReadAssemblyDefinition>();
        readAssemblyDefinition.Invoke(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);
        var writeAssemblyDefinition = Substitute.For<WriteAssemblyDefinition>();
        writeAssemblyDefinition.When(o => o.Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>()))
                               .Do(o => fileSystem.Add(o.Arg<string>(), "Patched DLL"));
                               
        var applyPatches = Factory(logger, fileSystem, readAssemblyDefinition, writeAssemblyDefinition);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        // Act
        applyPatches(_ModDefinition, new TypePatcherInfo(typeof(IHarmonyPlugin), TestPluginPatcher.Factory));

        // Assert
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", Arg.Any<string>(), _ModDefinition.Identifier);
        logger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(3);

        readAssemblyDefinition.Received(1).Invoke(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        writeAssemblyDefinition.Received(1).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());

        fileSystem.File.Delete.Received(1).Invoke(AssemblyPath);
        fileSystem.File.Move.Received().Invoke(@"C:\Current\Mods\DummyMod\DummyMod.patched.dll", AssemblyPath);

        // verify assemblyDefinition.Dispose as called ...
        var imageField = typeof(ModuleDefinition).GetField("Image", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var image = imageField.GetValue(assemblyDefinition.MainModule)!; // Mono.Cecil.PE.Image
        var streamField = image.GetType().GetField("Stream", BindingFlags.Instance | BindingFlags.Public)!;
        var disposable = streamField.GetValue(image)!; // Mono.Disposable<System.IO.Stream>
        var valueField = disposable.GetType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var stream = (Stream)valueField.GetValue(disposable)!;
        stream.CanRead.Should().BeFalse();
        stream.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_ExtraInterface()
    {
        // Arrange
        const string source = """
                              using Railroader.ModManager.Interfaces;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public interface IExtra {}

                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IExtra
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                              }
                              """;

        var (assemblyDefinition, _) = TestUtils.BuildAssemblyDefinition(source);

        var fileSystem = new MemoryFs(@"\Current") {
            { AssemblyPath, "", _OldDate },
            { @"C:\Current\Mods\DummyMod\source.cs", "", _NewDate },
            { @"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate }
        };

        var assemblyCompiler = Substitute.For<CompileAssemblyDelegate>();
        assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));

        var logger                 = Substitute.For<ILogger>();
        var readAssemblyDefinition = Substitute.For<ReadAssemblyDefinition>();
        readAssemblyDefinition.Invoke(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);
        var writeAssemblyDefinition = Substitute.For<WriteAssemblyDefinition>();
        writeAssemblyDefinition.When(o => o.Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>()))
                               .Do(o => fileSystem.Add(o.Arg<string>(), "Patched DLL"));
                               
        var applyPatches = Factory(logger, fileSystem, readAssemblyDefinition, writeAssemblyDefinition);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        // Act
        applyPatches(_ModDefinition, new TypePatcherInfo(typeof(IHarmonyPlugin), TestPluginPatcher.Factory));


        // Assert
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
        logger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(3);

        readAssemblyDefinition.Received(1).Invoke(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        writeAssemblyDefinition.Received(0).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_HandleThrowingPatcher1()
    {
        // Arrange

        const string source = """
                              using Railroader.ModManager.Interfaces;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                              }
                              """;

        var (assemblyDefinition, _) = TestUtils.BuildAssemblyDefinition(source);

        var fileSystem = new MemoryFs(@"\Current") {
            { AssemblyPath, "", _OldDate },
            { @"C:\Current\Mods\DummyMod\source.cs", "", _NewDate },
            { @"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate }
        };

        var assemblyCompiler = Substitute.For<CompileAssemblyDelegate>();
        assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));

        var logger                 = Substitute.For<ILogger>();
        var readAssemblyDefinition = Substitute.For<ReadAssemblyDefinition>();
        readAssemblyDefinition.Invoke(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);
        var writeAssemblyDefinition = Substitute.For<WriteAssemblyDefinition>();
        writeAssemblyDefinition.When(o => o.Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>()))
                               .Do(o => fileSystem.Add(o.Arg<string>(), "Patched DLL"));
                               
        var applyPatches = Factory(logger, fileSystem, readAssemblyDefinition, writeAssemblyDefinition);


        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        // Act
        applyPatches(_ModDefinition, new TypePatcherInfo(typeof(IHarmonyPlugin), ThrowingPatcher.Factory));

        // Assert
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Error(Arg.Is<Exception>(o => o.Message == "ThrowingPatcher"), "Failed to patch type {TypeName} for mod {ModId}", "Foo.Bar.FirstPlugin", _ModDefinition.Identifier);
        logger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(4);

        readAssemblyDefinition.Received(1).Invoke(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        writeAssemblyDefinition.Received(0).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_HandleThrowingPatcher2()
    {
        // Arrange
        const string source = """
                              using System;
                              using Railroader.ModManager.Interfaces;
                              using Railroader.ModManager.Tests.Features;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                              }
                              """;

        var (assemblyDefinition, _) = TestUtils.BuildAssemblyDefinition(source);

        var fileSystem = new MemoryFs(@"\Current") {
            { AssemblyPath, "", _OldDate },
            { @"C:\Current\Mods\DummyMod\source.cs", "", _NewDate },
            { @"C:\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate }
        };

        var assemblyCompiler = Substitute.For<CompileAssemblyDelegate>();
        assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));

        var logger                 = Substitute.For<ILogger>();
        var readAssemblyDefinition = Substitute.For<ReadAssemblyDefinition>();
        readAssemblyDefinition.Invoke(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);
        var writeAssemblyDefinition = Substitute.For<WriteAssemblyDefinition>();
        writeAssemblyDefinition.When(o => o.Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>()))
                               .Do(o => fileSystem.Add(o.Arg<string>(), "Patched DLL"));
                               
        var applyPatches = Factory(logger, fileSystem, readAssemblyDefinition, writeAssemblyDefinition);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        // Act
        applyPatches(_ModDefinition,
            new TypePatcherInfo(typeof(IHarmonyPlugin), TestPluginPatcher.Factory),
            new TypePatcherInfo(typeof(IHarmonyPlugin), ThrowingPatcher.Factory)
        );

        // Assert
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Error(Arg.Any<Exception>(), "Failed to patch type {TypeName} for mod {ModId}", "Foo.Bar.FirstPlugin", _ModDefinition.Identifier);
        logger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
        logger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(4);

        readAssemblyDefinition.Received(1).Invoke(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        writeAssemblyDefinition.Received(0).Invoke(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
    }

    private static class TestPluginPatcher
    {
        public static TypePatcherDelegate Factory() => (_, _) => true;
    }

    private static class ThrowingPatcher
    {
        public static TypePatcherDelegate Factory() => (_, _) => throw new Exception("ThrowingPatcher");
    }
}
