using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MemoryFileSystem;
using MemoryFileSystem.Types;
using NSubstitute;
using Railroader.ModManager.Features;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Tests.TestExtensions;
using Serilog;
using Shouldly;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsCodeCompiler
{
    private static readonly DateTime _OldDate = new(2000, 1, 2);
    private static readonly DateTime _NewDate = new(2000, 1, 4);

    private const string AssemblyPath = @"C:\Current\Mods\DummyMod\DummyMod.dll";

    private static readonly ModDefinition _ModDefinition = new() {
        Identifier = "DummyMod",
        Name = "Dummy Mod Name",
        BasePath = @"C:\Current\Mods\DummyMod"
    };

    private static CompileModAction CompileModFactory(ILogger logger, AssemblyCompilerDelegate compileAssembly, MemoryFs fileSystem) =>
        (definition, names) => CodeCompiler.CompileMod(logger,
            compileAssembly,
            fileSystem.DirectoryInfo,
            fileSystem.Directory.GetCurrentDirectory,
            fileSystem.File.Exists,
            fileSystem.File.GetLastWriteTime,
            fileSystem.File.Delete,
            definition,
            names ?? CodeCompiler.DefaultReferenceNames
        );

    [Fact]
    public void CompileMod_WhenNoSources() {
        // Arrange
        var logger          = Substitute.For<ILogger>();
        var compileAssembly = Substitute.For<AssemblyCompilerDelegate>();
        var fileSystem = new MemoryFs {
            @"C:\Current\Mods\DummyMod"
        };
        var compileMod      = CompileModFactory(logger, compileAssembly, fileSystem);

        // Act
        var actual = compileMod(_ModDefinition);

        // Assert
        actual.ShouldBe(CompileModResult.None);

        logger.ShouldReceiveNoCalls();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CompileMod_AssemblyUpToDate(int day) {
        // Arrange.
        var logger          = Substitute.For<ILogger>();
        var compileAssembly = Substitute.For<AssemblyCompilerDelegate>();
        var fileSystem = new MemoryFs {
            { AssemblyPath, "DLL", new DateTime(2000, 1, 2) },
            { @"C:\Current\Mods\DummyMod\source.cs", "", new DateTime(2000, 1, day) }
        };
        var compileMod = CompileModFactory(logger, compileAssembly, fileSystem);


        // Act
        var actual = compileMod(_ModDefinition);

        // Assert
        actual.ShouldBe(CompileModResult.Skipped);

        logger.Received().Information("Using existing mod {ModId} DLL at {Path}", _ModDefinition.Identifier, AssemblyPath);
        logger.ShouldReceiveCallCount(1);
    }

    [Fact]
    public void CompileMod_Compilation_Failed() {
        // Arrange
        var logger          = Substitute.For<ILogger>();
        var compileAssembly = Substitute.For<AssemblyCompilerDelegate>();
        compileAssembly.Invoke(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>()).Returns(_ => false);

        var fileSystem = new MemoryFs(@"C:\Current") {
            { AssemblyPath, "DLL", _OldDate },
            { @"C:\Current\Mods\DummyMod\source1.cs", "", _NewDate },
            { @"C:\Current\Mods\DummyMod\source2.cs", "", _OldDate }
        };
        var compileMod = CompileModFactory(logger, compileAssembly, fileSystem);

        string[] sources = [@"C:\Current\Mods\DummyMod\source1.cs", @"C:\Current\Mods\DummyMod\source2.cs"];
        string[] references = [
            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
            @"C:\Current\Railroader_Data\Managed\Railroader.ModManager.Interfaces.dll",
            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll"
        ];

        // Act
        var actual = compileMod(_ModDefinition);

        // Assert
        actual.ShouldBe(CompileModResult.Error);

        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);

        compileAssembly.Received().Invoke(AssemblyPath,
            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
            Arg.Is<string[]>(o => o.SequenceEqual(references))
        );

        logger.Received().Error("Compilation failed for mod {ModId} ...", _ModDefinition.Identifier);
        logger.ShouldReceiveCallCount(3);

        fileSystem.File.Delete.Received().Invoke(AssemblyPath);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompileMod_Compilation_Successful(bool hasEmptyRequires)
    {
        // Arrange
        var logger          = Substitute.For<ILogger>();
        var compileAssembly = Substitute.For<AssemblyCompilerDelegate>();
        
        var fileSystem = new MemoryFs(@"C:\Current") {
            { AssemblyPath, "DLL", _OldDate },
            { @"C:\Current\Mods\DummyMod\source1.cs", "", _NewDate },
            { @"C:\Current\Mods\DummyMod\source2.cs", "", _OldDate }
        };
        var compileMod = CompileModFactory(logger, compileAssembly, fileSystem);

        compileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>())
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));

        var modDefinition = new ModDefinition
        {
            Identifier = "DummyMod",
            Name = "Dummy Mod Name",
            BasePath = @"C:\Current\Mods\DummyMod",
            Requires = hasEmptyRequires ? new Dictionary<string, FluentVersion?>() : null
        };

        string[] sources = [@"C:\Current\Mods\DummyMod\source1.cs", @"C:\Current\Mods\DummyMod\source2.cs"];
        string[] references = [
            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
            @"C:\Current\Railroader_Data\Managed\Railroader.ModManager.Interfaces.dll",
            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll"
        ];
        
        // Act
        var actual = compileMod(modDefinition);

        // Assert
        actual.ShouldBe(CompileModResult.Success);

        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);

        compileAssembly.Received().Invoke(AssemblyPath,
            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
            Arg.Is<string[]>(o => o.SequenceEqual(references))
        );

        logger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
        logger.ShouldReceiveCallCount(3);

        fileSystem.File.Delete.Received().Invoke(AssemblyPath);
        fileSystem.Items.ShouldContainKeyWhereValue(AssemblyPath, o => o.ShouldBeEquivalentTo(new MemoryEntry(AssemblyPath, Encoding.UTF8.GetBytes("Compiled DLL"))));
    }

    [Fact]
    public void CompileMod_Compilation_WithValidModReferences()
    {
        // Arrange
        var logger          = Substitute.For<ILogger>();
        var compileAssembly = Substitute.For<AssemblyCompilerDelegate>();
        
        var fileSystem = new MemoryFs(@"C:\Current") {
            { AssemblyPath, "DLL", _OldDate },
            { @"C:\Current\Mods\DummyMod\source.cs", "", _NewDate },
            { @"C:\Current\Mods\DepMod1\DepMod1.dll", "", _OldDate },
            { @"C:\Current\Mods\DepMod2\DepMod2.dll", "", _OldDate },
        };
        var compileMod = CompileModFactory(logger, compileAssembly, fileSystem);

        compileAssembly(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>())
            .Returns(_ => true)
            .AndDoes(o => fileSystem.Add(o.ArgAt<string>(0), "Compiled DLL"));


        var modDefinition = new ModDefinition
        {
            Identifier = "DummyMod",
            Name = "Dummy Mod Name",
            BasePath = @"C:\Current\Mods\DummyMod",
            Requires = new Dictionary<string, FluentVersion?>()
        };
        modDefinition.Requires.Add("DepMod1", null);
        modDefinition.Requires.Add("DepMod2", null);

        string[] sources = [@"C:\Current\Mods\DummyMod\source.cs"];
        string[] expectedReferences = [
            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
            @"C:\Current\Railroader_Data\Managed\Railroader.ModManager.Interfaces.dll",
            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll",
            @"C:\Current\Mods\DepMod1\DepMod1.dll",
            @"C:\Current\Mods\DepMod2\DepMod2.dll"
        ];

        string[] expectedRequiredMods = ["DepMod1", "DepMod2"];

        // Act
        var actual = compileMod(modDefinition);

        // Assert
        actual.ShouldBe(CompileModResult.Success);

        logger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", modDefinition.Identifier, AssemblyPath);
        logger.Received().Information("Compiling mod {ModId} ...", modDefinition.Identifier);
        logger.Received().Information("Adding references to {Mods} ...", Arg.Is<ICollection<string>>(o => o.SequenceEqual(expectedRequiredMods)));
        logger.Received().Information("Compilation complete for mod {ModId}", modDefinition.Identifier);
        logger.ShouldReceiveCallCount(4);

        compileAssembly.Received().Invoke(AssemblyPath,
            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
            Arg.Is<string[]>(o => o.SequenceEqual(expectedReferences))
        );

        fileSystem.File.Delete.Received().Invoke(AssemblyPath);

        fileSystem.Items.ShouldContainKeyWhereValue(AssemblyPath, o=>o.ShouldBeEquivalentTo(new MemoryEntry(AssemblyPath, Encoding.UTF8.GetBytes("Compiled DLL"))));
    }
}
