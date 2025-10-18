using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Mono.Cecil;
using NSubstitute;
using Railroader_ModInterfaces.Tests.Wrappers.FileSystemWrapper;
using Railroader.ModInjector;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Railroader.ModInjector.PluginPatchers;
using Serilog;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace Railroader_ModInterfaces.Tests.Services;

public sealed class CodeCompilerTests
{
    private static readonly DateTime _OldDate = new(2000, 1, 2);
    private static readonly DateTime _NewDate = new(2000, 1, 3);

    private static readonly ModDefinition _ModDefinition = new()
    {
        Identifier = "DummyMod",
        Name = "Dummy Mod Name",
        BasePath = @"\Current\Mods\DummyMod"
    };

    [Fact]
    public void CompileMod_WhenNoSources() {
        // Arrange
        var fileSystem                = new MockFileSystem();
        var assemblyCompiler          = Substitute.For<IAssemblyCompiler>();
        var logger                    = Substitute.For<ILogger>();
        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        var sut = new CodeCompiler {
            FileSystem = fileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().BeNull();

        logger.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void CompileMod_AssemblyUpToDate() {
        // Arrange
        const string assemblyPath = @"\Current\Mods\DummyMod\DummyMod.dll";
        var fileSystem   = new MockFileSystem {
            new MockFileSystemFile(assemblyPath, "", _NewDate),
            new MockFileSystemFile(@"\Current\Mods\DummyMod\source.cs", "", _OldDate)
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger                    = Substitute.For<ILogger>();
        
        var assemblyCompiler          = Substitute.For<IAssemblyCompiler>();
        
        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        var sut = new CodeCompiler {
            FileSystem = fileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(assemblyPath);

        logger.Received().Information("Using existing mod {ModId} DLL at {Path}", _ModDefinition.Identifier, assemblyPath);
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompileMod_Compilation(bool success) {
        // Arrange
        const string assemblyPath = @"\Current\Mods\DummyMod\DummyMod.dll";
        var fileSystem   = new MockFileSystem {
            new MockFileSystemFile(assemblyPath, "", _OldDate),
            new MockFileSystemFile(@"\Current\Mods\DummyMod\source.cs", "", _NewDate)
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger                    = Substitute.For<ILogger>();
        var assemblyCompiler          = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(success);

        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        var sut = new CodeCompiler {
            FileSystem = fileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [],
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(success ? assemblyPath : null!);

        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, assemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);

        if (success) {
            logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        } else {
            logger.Received().Error("Compilation failed for mod {ModId} ...", _ModDefinition.Identifier);
        }

        logger.ReceivedCalls().Should().HaveCount(3);
    }
    
    [Fact]
    public void CompileMod_Compilation_WithPatches_AssemblyLoadFail() {
         // Arrange
        const string assemblyPath = @"\Current\Mods\DummyMod\DummyMod.dll";
        var fileSystem   = new MockFileSystem {
            new MockFileSystemFile(assemblyPath, "", _OldDate),
            new MockFileSystemFile(@"\Current\Mods\DummyMod\source.cs", "", _NewDate)
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        
        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(o => null);

        var assemblyCompiler          = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = fileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers =[(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))],
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().BeNull();
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, assemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", assemblyPath, _ModDefinition.Identifier);
        logger.Received().Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, _ModDefinition.Identifier);
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

        var outputPath         = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "CodeCompilerTests", "CompileMod_Compilation_WithPatches_ReturnValidInstances");
        var assemblyDefinition = AssemblyTestUtils.BuildAssemblyDefinition(source, outputPath);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"\Current\Railroader_Data\Managed",
            @"\Current\Mods\SecondMod",
        ];

        const string assemblyPath = @"\Current\Mods\DummyMod\DummyMod.dll";
        var fileSystem   = new MockFileSystem {
            new MockFileSystemFile(assemblyPath, "", _OldDate),
            new MockFileSystemFile(@"\Current\Mods\DummyMod\source.cs", "", _NewDate),
            new MockFileSystemFile(@"\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate)
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        
        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);

        var assemblyCompiler          = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = fileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [(typeof(IHarmonyPlugin), typeof(TestPluginPatcher))],
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(assemblyPath);
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, assemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Debug("TestPluginPatcher::Patch | typeDefinition: {type}", "FirstPlugin");
        logger.Received().Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", Arg.Any<string>(), _ModDefinition.Identifier);
        logger.Received().Information("Patching complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(7);

        assemblyDefinitionWrapper.Received(1).ReadAssembly(assemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        assemblyDefinitionWrapper.Received(1).Write(Arg.Any<AssemblyDefinition>(), Arg.Any<string>());
    }

    [Fact]
    public void CompileMod_Compilation_WithPatches_HandleThrowingPatcher() {
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

        var outputPath         = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "CodeCompilerTests", "CompileMod_Compilation_WithPatches_HandleThrowingPatcher");
        var assemblyDefinition = AssemblyTestUtils.BuildAssemblyDefinition(source, outputPath);

        string[] expectedDirectories = [
            ".",
            "bin",
            @"\Current\Railroader_Data\Managed",
            @"\Current\Mods\SecondMod",
        ];

        const string assemblyPath = @"\Current\Mods\DummyMod\DummyMod.dll";
        var fileSystem   = new MockFileSystem {
            new MockFileSystemFile(assemblyPath, "", _OldDate),
            new MockFileSystemFile(@"\Current\Mods\DummyMod\source.cs", "", _NewDate),
            new MockFileSystemFile(@"\Current\Mods\SecondMod\SecondMod.dll", "", _OldDate)
        };
        fileSystem.CurrentDirectory = @"\Current";

        var logger = Substitute.For<ILogger>();
        
        var assemblyDefinitionWrapper = Substitute.For<IAssemblyDefinitionWrapper>();
        assemblyDefinitionWrapper.ReadAssembly(Arg.Any<string>(), Arg.Any<ReaderParameters>()).Returns(_ => assemblyDefinition);

        var assemblyCompiler          = Substitute.For<IAssemblyCompiler>();
        assemblyCompiler.CompileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(true);

        var sut = new CodeCompiler {
            FileSystem = fileSystem,
            Logger = logger,
            AssemblyCompiler = assemblyCompiler,
            AssemblyDefinitionWrapper = assemblyDefinitionWrapper,
            PluginPatchers = [(typeof(IHarmonyPlugin), typeof(ThrowingPatcher))],
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(assemblyPath);
        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, assemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.Received().Information("Patching mod {ModId} ...", _ModDefinition.Identifier);
        logger.Received().Error(Arg.Is<Exception>(o => o.Message == "ThrowingPatcher"), "Failed to patch type {TypeName} for mod {ModId}", "Foo.Bar.FirstPlugin", _ModDefinition.Identifier);
        logger.Received().Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", assemblyPath, _ModDefinition.Identifier);
        logger.ReceivedCalls().Should().HaveCount(7);

        assemblyDefinitionWrapper.Received(1).ReadAssembly(assemblyPath,
            Arg.Is<ReaderParameters>(o =>
                o.AssemblyResolver is DefaultAssemblyResolver &&
                ((DefaultAssemblyResolver)o.AssemblyResolver).GetSearchDirectories()!.SequenceEqual(expectedDirectories)
            )
        );
        assemblyDefinitionWrapper.ReceivedCalls().Should().HaveCount(1);
    }


    private sealed class TestPluginPatcher(ILogger logger) : IPluginPatcher
    {
        public void Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) {
            logger.Debug("TestPluginPatcher::Patch | typeDefinition: {type}", typeDefinition.Name);
        }
    }

    private sealed class ThrowingPatcher(ILogger logger) : IPluginPatcher
    {
        public void Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) {
            throw new Exception("ThrowingPatcher");
        }
    }
}

