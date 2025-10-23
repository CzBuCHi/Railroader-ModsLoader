using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Mono.Cecil;
using NSubstitute;
using NSubstitute.FileSystem;
using Railroader.ModInjector;
using Railroader.ModInjector.Patchers;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace Railroader_ModInterfaces.Tests.Services;

public sealed class CodeCompilerTests
{
    private static readonly DateTime _OldDate = new(2000, 1, 2);
    private static readonly DateTime _NewDate = new(2000, 1, 4);

    private const string AssemblyPath = @"C:\Current\Mods\DummyMod\DummyMod.dll";

    private static readonly ModDefinition _ModDefinition = new() {
        Identifier = "DummyMod",
        Name = "Dummy Mod Name",
        BasePath = @"C:\Current\Mods\DummyMod"
    };

    [Fact]
    public void CompileMod_WhenNoSources() {
        // Arrange
        var memory                    = new MemoryFileSystem();
        var assemblyCompiler          = Substitute.For<IAssemblyCompiler>();
        var logger                    = Substitute.For<ILogger>();
        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper
        };

        IEnumerable<string> defaultReferences = [
            "Assembly-CSharp",
            "0Harmony",
            "Railroader-ModInterfaces",
            "Serilog",
            "UnityEngine.CoreModule"
        ];

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        sut.ReferenceNames.Should().BeEquivalentTo(defaultReferences, o => o.WithStrictOrdering());

        actual.Should().BeNull();

        logger.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CompileMod_AssemblyUpToDate(int day) {
        // Arrange
        var memory = new MemoryFileSystem(@"\Current") {
            (AssemblyPath, new DateTime(2000, 1, 2), "DLL"),
            (@"C:\Current\Mods\DummyMod\source.cs", new DateTime(2000, 1, day), "")
        };

        var logger = Substitute.For<ILogger>();

        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(AssemblyPath);

        logger.Received().Information("Using existing mod {ModId} DLL at {Path}", _ModDefinition.Identifier, AssemblyPath);
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void CompileMod_Compilation_Failed() {
        // Arrange
        var memory = new MemoryFileSystem(@"C:\Current") {
            (AssemblyPath, _OldDate, ""),
            (@"C:\Current\Mods\DummyMod\source1.cs", _NewDate, ""),
            (@"C:\Current\Mods\DummyMod\source2.cs", _OldDate, "")
        };

        var logger           = Substitute.For<ILogger>();
        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(_ => false);

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = []
        };

        string[] sources = [@"C:\Current\Mods\DummyMod\source1.cs", @"C:\Current\Mods\DummyMod\source2.cs"];
        string[] references = [
            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
            @"C:\Current\Railroader_Data\Managed\Railroader-ModInterfaces.dll",
            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll"
        ];

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().BeNull();

        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);

        assemblyCompiler.Received().CompileAssembly(AssemblyPath,
            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
            Arg.Is<string[]>(o => o.SequenceEqual(references))
        );

        logger.Received().Error("Compilation failed for mod {ModId} ...", _ModDefinition.Identifier);

        logger.ReceivedCalls().Should().HaveCount(3);

        memory.FileSystem.File.Received().Delete(AssemblyPath);
    }

    [Fact]
    public void CompileMod_Compilation_Successful() {
        // Arrange
        var memory = new MemoryFileSystem(@"C:\Current") {
            (AssemblyPath, _OldDate, ""),
            (@"C:\Current\Mods\DummyMod\source1.cs", _NewDate, ""),
            (@"C:\Current\Mods\DummyMod\source2.cs", _OldDate, "")
        };

        var logger           = Substitute.For<ILogger>();
        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();

        string[] sources = [@"C:\Current\Mods\DummyMod\source1.cs", @"C:\Current\Mods\DummyMod\source2.cs"];
        string[] references = [
            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
            @"C:\Current\Railroader_Data\Managed\Railroader-ModInterfaces.dll",
            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll"
        ];

        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(_ => true).AndDoes(_ => { memory.Add((AssemblyPath, "Compiled DLL")); });

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = []
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(AssemblyPath);

        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);

        assemblyCompiler.Received().CompileAssembly(AssemblyPath,
            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
            Arg.Is<string[]>(o => o.SequenceEqual(references))
        );

        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);

        logger.ReceivedCalls().Should().HaveCount(3);

        memory.FileSystem.File.Received().Delete(AssemblyPath);
        memory.Should().ContainEquivalentOf(new MemoryEntry(AssemblyPath, false, MemoryFileSystem.First, "Compiled DLL", null));
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_AssemblyLoadFail() {
        // Arrange
        var memory = new MemoryFileSystem(@"C:\Current") {
            (AssemblyPath, _OldDate, ""),
            (@"C:\Current\Mods\DummyMod\source.cs", _NewDate, "")
        };

        var logger = Substitute.For<ILogger>();

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => null);

        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))]
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().BeNull();
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", AssemblyPath, _ModDefinition.Identifier);
        logger.Received().Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", AssemblyPath, _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(6);
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_ReturnValidInstances() {
        // Arrange
        const string source = """
                              using Railroader.ModInterfaces;
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

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinition(source);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        var memory = new MemoryFileSystem(@"C:\Current") {
            (AssemblyPath, _OldDate, ""),
            (@"C:\Current\Mods\DummyMod\source.cs", _NewDate, ""),
            (@"C:\Current\Mods\SecondMod\SecondMod.dll", _OldDate, "")
        };

        var logger = Substitute.For<ILogger>();

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);
        assemblyDefinitionWrapper.When(o => o.Write(Arg.Any<AssemblyDefinition>(), Arg.Any<string>())).Do(o => {
            var       definition   = o.Arg<AssemblyDefinition>();
            var       fileName     = o.Arg<string>();
            using var stream       = memory.FileSystem.File.Create(fileName);
            using var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true);
            streamWriter.Write(definition.Name!.Name!);
        });

        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))]
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);
        AssemblyTestUtils.Write(assemblyDefinition, outputPath, "patched");

        // Assert
        actual.Should().Be(AssemblyPath);
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Debug("TestPluginPatcher::Patch | typeDefinition: {type}", "FirstPlugin");
        logger.Received().Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", Arg.Any<string>(), _ModDefinition.Identifier);
        logger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(7);

        assemblyDefinitionWrapper.Received(1).ReadAssembly(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        assemblyDefinitionWrapper.Received(1).Write(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());

        memory.FileSystem.File.Received(2).Delete(AssemblyPath);
        memory.FileSystem.File.Received().Move(@"C:\Current\Mods\DummyMod\DummyMod.patched.dll", AssemblyPath);
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_ExtraInterface() {
        // Arrange
        const string source = """
                              using Railroader.ModInterfaces;
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

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinition(source);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        var memory = new MemoryFileSystem(@"C:\Current") {
            (AssemblyPath, _OldDate, ""),
            (@"C:\Current\Mods\DummyMod\source.cs", _NewDate, ""),
            (@"C:\Current\Mods\SecondMod\SecondMod.dll", _OldDate, "")
        };

        var logger = Substitute.For<ILogger>();

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);

        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))]
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);
        AssemblyTestUtils.Write(assemblyDefinition, outputPath, "patched");

        // Assert
        actual.Should().Be(AssemblyPath);
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
        logger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(6);

        assemblyDefinitionWrapper.Received(1).ReadAssembly(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        assemblyDefinitionWrapper.Received(0).Write(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_HandleThrowingPatcher1() {
        // Arrange
        const string source = """
                              using Railroader.ModInterfaces;
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

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinition(source);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        var memory = new MemoryFileSystem(@"C:\Current") {
            (AssemblyPath, _OldDate, ""),
            (@"C:\Current\Mods\DummyMod\source.cs", _NewDate, ""),
            (@"C:\Current\Mods\SecondMod\SecondMod.dll", _OldDate, "")
        };

        var logger = Substitute.For<ILogger>();

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);

        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [(typeof(IHarmonyPlugin), typeof(ThrowingPatcher))]
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);
        AssemblyTestUtils.Write(assemblyDefinition, outputPath, "patched");

        // Assert
        actual.Should().Be(AssemblyPath);
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Error(Arg.Is<Exception>(o => o.Message == "ThrowingPatcher"), "Failed to patch type {TypeName} for mod {ModId}", "Foo.Bar.FirstPlugin", _ModDefinition.Identifier);
        logger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(7);

        assemblyDefinitionWrapper.Received(1).ReadAssembly(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        assemblyDefinitionWrapper.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_HandleThrowingPatcher2() {
        // Arrange
        const string source = """
                              using System;
                              using Railroader.ModInterfaces;
                              using Railroader_ModInterfaces.Tests.Services;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin, ITopRightButtonPlugin
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                      
                                      public string IconName => "";
                                      public string Tooltip => "";
                                      public int Index => 0;
                                      public Action OnClick => () => {};
                                  }
                              }
                              """;

        var (assemblyDefinition, _) = AssemblyTestUtils.BuildAssemblyDefinition(source);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"C:\Current\Railroader_Data\Managed",
            @"C:\Current\Mods\SecondMod"
        ];

        var memory = new MemoryFileSystem(@"C:\Current") {
            (AssemblyPath, _OldDate, ""),
            (@"C:\Current\Mods\DummyMod\source.cs", _NewDate, ""),
            (@"C:\Current\Mods\SecondMod\SecondMod.dll", _OldDate, "")
        };

        var logger = Substitute.For<ILogger>();

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);

        var assemblyCompiler = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = memory.FileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [
                (typeof(IHarmonyPlugin), typeof(TestPluginPatcher)),
                (typeof(ITopRightButtonPlugin), typeof(ThrowingPatcher))
            ]
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(AssemblyPath);
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Debug("TestPluginPatcher::Patch | typeDefinition: {type}", "FirstPlugin");
        logger.Received().Error(Arg.Any<Exception>(), "Failed to patch type {TypeName} for mod {ModId}", "Foo.Bar.FirstPlugin", _ModDefinition.Identifier);
        logger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", AssemblyPath, _ModDefinition.Identifier);
        logger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(8);

        assemblyDefinitionWrapper.Received(1).ReadAssembly(AssemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        assemblyDefinitionWrapper.ReceivedCalls().Should().HaveCount(1);
    }

    private sealed class TestPluginPatcher(ILogger logger) : IMethodPatcher
    {
        public bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) {
            logger.Debug("TestPluginPatcher::Patch | typeDefinition: {type}", typeDefinition.Name);
            return true;
        }
    }

#pragma warning disable CS9113 // Parameter is unread.
    private sealed class ThrowingPatcher(ILogger logger) : IMethodPatcher
#pragma warning restore CS9113 // Parameter is unread.
    {
        public bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) => throw new Exception("ThrowingPatcher");
    }
}
