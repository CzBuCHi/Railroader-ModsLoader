//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using FluentAssertions;
//using MemoryFileSystem.Internal;
//using NSubstitute;
//using Railroader.ModManager.Interfaces;
//using Railroader.ModManager.Services;

//namespace Railroader.ModManager.Tests.Services;

//public sealed class TestsCodeCompiler
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
//    public void CompileMod_WhenNoSources() {
//        // Arrange
//        var serviceManager =
//            new TestServiceManager()
//                .WithAssemblyCompiler();

//        var sut = serviceManager.CreateCodeCompiler();

//        IEnumerable<string> defaultReferences = [
//            "Assembly-CSharp",
//            "0Harmony",
//            "Railroader.ModManager.Interfaces",
//            "Serilog",
//            "UnityEngine.CoreModule"
//        ];

//        // Act
//        var actual = sut.CompileMod(_ModDefinition);

//        // Assert
//        sut.ReferenceNames.Should().BeEquivalentTo(defaultReferences);

//        actual.Should().BeNull();

//        serviceManager.MainLogger.ReceivedCalls().Should().BeEmpty();
//    }

//    [Theory]
//    [InlineData(1)]
//    [InlineData(2)]
//    public void CompileMod_AssemblyUpToDate(int day) {
//        // Arrange
//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "DLL", new DateTime(2000, 1, 2))
//                .WithFile(@"C:\Current\Mods\DummyMod\source.cs", "", new DateTime(2000, 1, day))
//                .WithAssemblyCompiler();
//        var sut = serviceManager.CreateCodeCompiler();

//        // Act
//        var actual = sut.CompileMod(_ModDefinition);

//        // Assert
//        actual.Should().Be(AssemblyPath);

//        serviceManager.MainLogger.Received().Information("Using existing mod {ModId} DLL at {Path}", _ModDefinition.Identifier, AssemblyPath);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(1);
//    }

//    [Fact]
//    public void CompileMod_Compilation_Failed() {
//        // Arrange
//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source1.cs", "", _NewDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source2.cs", "", _OldDate)
//                .WithAssemblyCompiler(assemblyCompiler => { assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _).Returns(_ => false); });

//        var sut = serviceManager.CreateCodeCompiler();

//        string[] sources = [@"C:\Current\Mods\DummyMod\source1.cs", @"C:\Current\Mods\DummyMod\source2.cs"];
//        string[] references = [
//            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
//            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
//            @"C:\Current\Railroader_Data\Managed\Railroader.ModManager.Interfaces.dll",
//            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
//            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll"
//        ];

//        // Act
//        var actual = sut.CompileMod(_ModDefinition);

//        // Assert
//        actual.Should().BeNull();

//        serviceManager.MainLogger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
//        serviceManager.MainLogger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);

//        serviceManager.GetService<CompileAssemblyDelegate>().Received().Invoke(AssemblyPath,
//            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
//            Arg.Is<string[]>(o => o.SequenceEqual(references)),
//            out Arg.Any<string>()
//        );

//        serviceManager.MainLogger.Received().Error("Compilation failed for mod {ModId} ...", _ModDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(3);

//        serviceManager.MemoryFs.FileSystem.File.Received().Delete(AssemblyPath);
//    }

//    [Theory]
//    [InlineData(true)]
//    [InlineData(false)]
//    public void CompileMod_Compilation_Successful(bool hasEmptyRequires) {
//        // Arrange
//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source1.cs", "", _NewDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source2.cs", "", _OldDate);

//        serviceManager.WithAssemblyCompiler(assemblyCompiler => {
//            assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
//                            .Returns(_ => true)
//                            .AndDoes(o => serviceManager.MemoryFs.Add(o.ArgAt<string>(0), "Compiled DLL"));
//        });

//        var modDefinition = new ModDefinition {
//            Identifier = "DummyMod",
//            Name = "Dummy Mod Name",
//            BasePath = @"C:\Current\Mods\DummyMod",
//            Requires = hasEmptyRequires ? new Dictionary<string, FluentVersion?>() : null
//        };

//        string[] sources = [@"C:\Current\Mods\DummyMod\source1.cs", @"C:\Current\Mods\DummyMod\source2.cs"];
//        string[] references = [
//            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
//            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
//            @"C:\Current\Railroader_Data\Managed\Railroader.ModManager.Interfaces.dll",
//            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
//            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll"
//        ];

//        var sut = serviceManager.CreateCodeCompiler();

//        // Act
//        var actual = sut.CompileMod(modDefinition);

//        // Assert
//        actual.Should().Be(AssemblyPath);

//        serviceManager.MainLogger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", _ModDefinition.Identifier, AssemblyPath);
//        serviceManager.MainLogger.Received().Information("Compiling mod {ModId} ...", _ModDefinition.Identifier);

//        serviceManager.GetService<CompileAssemblyDelegate>().Received().Invoke(AssemblyPath,
//            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
//            Arg.Is<string[]>(o => o.SequenceEqual(references)),
//            out Arg.Any<string>()
//        );

//        serviceManager.MainLogger.Received().Information("Compilation complete for mod {ModId}", _ModDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(3);

//        serviceManager.MemoryFs.FileSystem.File.Received().Delete(AssemblyPath);
//        serviceManager.MemoryFs.Items.Should().ContainKey(AssemblyPath).WhoseValue
//                      .Should().BeEquivalentTo(new MemoryEntry(AssemblyPath, Encoding.UTF8.GetBytes("Compiled DLL")));
//    }

//    [Fact]
//    public void CompileMod_Compilation_WithValidModReferences() {
//        // Arrange
//        var serviceManager =
//            new TestServiceManager(@"\Current")
//                .WithFile(AssemblyPath, "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DummyMod\source.cs", "", _NewDate)
//                .WithFile(@"C:\Current\Mods\DepMod1\DepMod1.dll", "", _OldDate)
//                .WithFile(@"C:\Current\Mods\DepMod2\DepMod2.dll", "", _OldDate);

//        serviceManager.WithAssemblyCompiler(assemblyCompiler => {
//            assemblyCompiler(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), out _)
//                            .Returns(_ => true)
//                            .AndDoes(o => serviceManager.MemoryFs.Add(o.ArgAt<string>(0), "Compiled DLL"));
//        });

//        var modDefinition = new ModDefinition {
//            Identifier = "DummyMod",
//            Name = "Dummy Mod Name",
//            BasePath = @"C:\Current\Mods\DummyMod",
//            Requires = new Dictionary<string, FluentVersion?>()
//        };
//        modDefinition.Requires.Add("DepMod1", null);
//        modDefinition.Requires.Add("DepMod2", null);

//        var sut = serviceManager.CreateCodeCompiler();

//        string[] sources = [@"C:\Current\Mods\DummyMod\source.cs"];
//        string[] expectedReferences = [
//            @"C:\Current\Railroader_Data\Managed\Assembly-CSharp.dll",
//            @"C:\Current\Railroader_Data\Managed\0Harmony.dll",
//            @"C:\Current\Railroader_Data\Managed\Railroader.ModManager.Interfaces.dll",
//            @"C:\Current\Railroader_Data\Managed\Serilog.dll",
//            @"C:\Current\Railroader_Data\Managed\UnityEngine.CoreModule.dll",
//            @"C:\Current\Mods\DepMod1\DepMod1.dll",
//            @"C:\Current\Mods\DepMod2\DepMod2.dll"
//        ];

//        string[] expectedRequiredMods = ["DepMod1", "DepMod2"];

//        // Act
//        var actual = sut.CompileMod(modDefinition);

//        // Assert
//        actual.Should().Be(AssemblyPath);

//        serviceManager.MainLogger.Received().Information("Deleting mod {ModId} DLL at {Path} because it is outdated", modDefinition.Identifier, AssemblyPath);
//        serviceManager.MainLogger.Received().Information("Compiling mod {ModId} ...", modDefinition.Identifier);
//        serviceManager.MainLogger.Received().Information("Adding references to {Mods} ...", Arg.Is<ICollection<string>>(o => o.SequenceEqual(expectedRequiredMods)));
//        serviceManager.MainLogger.Received().Information("Compilation complete for mod {ModId}", modDefinition.Identifier);
//        serviceManager.MainLogger.ReceivedCalls().Should().HaveCount(4);

//        serviceManager.GetService<CompileAssemblyDelegate>().Received().Invoke(AssemblyPath,
//            Arg.Is<string[]>(o => o.SequenceEqual(sources)),
//            Arg.Is<string[]>(o => o.SequenceEqual(expectedReferences)),
//            out Arg.Any<string>()
//        );

//        serviceManager.MemoryFs.FileSystem.File.Received().Delete(AssemblyPath);

//        serviceManager.MemoryFs.Items.Should().ContainKey(AssemblyPath).WhoseValue
//                      .Should().BeEquivalentTo(new MemoryEntry(AssemblyPath, Encoding.UTF8.GetBytes("Compiled DLL")));
//    }
//}
