using System;
using System.IO;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Serilog;

namespace Railroader_ModInterfaces.Tests.Services;

public sealed class CodeCompilerTests
{
    private static readonly DateTime _OldDate = new(2000, 1, 2);
    private static readonly DateTime _NewDate = new(2000, 1, 3);

    private const string OutputDllPath  = @"Mods\DummyMod\DummyMod.dll";
    private const string SourceFilePath = @"Mods\DummyMod\A.cs";

    private static readonly ModDefinition _ModDefinition = new() {
        Identifier = "DummyMod",
        Name = "Dummy Mod Name",
        BasePath = @"Mods\DummyMod\"
    };

    [Fact]
    public void CompileMod_WhenNoSources() {
        // Arrange
        var directoryInfo = Substitute.For<IDirectoryInfo>();
        directoryInfo.EnumerateFiles("*.cs", SearchOption.AllDirectories).Returns([]);

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.DirectoryInfo(_ModDefinition.BasePath).Returns(directoryInfo);

        var monoCompiler = Substitute.For<ICompilerCallableEntryPoint>();
        var logger       = Substitute.For<ILogger>();
        var sut          = new CodeCompiler(fileSystem, monoCompiler, logger);
        
        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().BeNull();

        fileSystem.ReceivedCalls().Should().HaveCount(1);
        fileSystem.Received().DirectoryInfo(_ModDefinition.BasePath);

        directoryInfo.ReceivedCalls().Should().HaveCount(1);
        directoryInfo.Received().EnumerateFiles("*.cs", SearchOption.AllDirectories);

        monoCompiler.ReceivedCalls().Should().HaveCount(0);

        logger.ReceivedCalls().Should().HaveCount(0);
    }

    [Fact]
    public void CompileMod_WhenDllValid() {
        // Arrange
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.LastWriteTime.Returns(_OldDate);

        var directoryInfo = Substitute.For<IDirectoryInfo>();
        directoryInfo.EnumerateFiles(Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([fileInfo]);

        var file = Substitute.For<IFile>();
        file.Exists(Arg.Any<string>()).Returns(false);
        file.Exists(OutputDllPath).Returns(true);
        file.GetLastWriteTime(OutputDllPath).Returns(_NewDate);

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.DirectoryInfo(Arg.Any<string>()).Returns(directoryInfo);
        fileSystem.File.Returns(file);

        var monoCompiler = Substitute.For<ICompilerCallableEntryPoint>();
        var logger       = Substitute.For<ILogger>();
        var sut          = new CodeCompiler(fileSystem, monoCompiler, logger);

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(OutputDllPath);

        fileSystem.ReceivedCalls().Should().HaveCount(3);
        fileSystem.Received().DirectoryInfo(_ModDefinition.BasePath);
        _ = fileSystem.Received(2).File;

        directoryInfo.ReceivedCalls().Should().HaveCount(1);
        directoryInfo.Received().EnumerateFiles("*.cs", SearchOption.AllDirectories);

        file.ReceivedCalls().Should().HaveCount(2);
        file.Received().Exists(OutputDllPath);
        file.Received().GetLastWriteTime(OutputDllPath);

        monoCompiler.ReceivedCalls().Should().HaveCount(0);

        logger.ReceivedCalls().Should().HaveCount(1);
        logger.Received().Information("Using existing mod {identifier} DLL ...", _ModDefinition.Identifier);
    }

    [Fact]
    public void CompileMod_WhenOutdated_AndCompilationFails() {
        // Arrange
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.LastWriteTime.Returns(_NewDate);
        fileInfo.FullName.Returns(SourceFilePath);

        var directoryInfo = Substitute.For<IDirectoryInfo>();
        directoryInfo.EnumerateFiles(Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([fileInfo]);

        var file = Substitute.For<IFile>();
        file.Exists(Arg.Any<string>()).Returns(false);
        file.Exists(OutputDllPath).Returns(true);
        file.GetLastWriteTime(OutputDllPath).Returns(_OldDate);

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.DirectoryInfo(Arg.Any<string>()).Returns(directoryInfo);
        fileSystem.File.Returns(file);

        var monoCompiler = Substitute.For<ICompilerCallableEntryPoint>();
        monoCompiler.InvokeCompiler(Arg.Any<string[]>(), Arg.Any<TextWriter>()).Returns(false).AndDoes(info => {
            var writer = info.ArgAt<TextWriter>(1);
            writer.Write("ERROR");
        });
        var logger = Substitute.For<ILogger>();
        var sut    = new CodeCompiler(fileSystem, monoCompiler, logger);

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().BeNull();

        fileSystem.ReceivedCalls().Should().HaveCount(8);
        fileSystem.Received().DirectoryInfo(_ModDefinition.BasePath);
        _ = fileSystem.Received(3).File;
        _ = fileSystem.Received(4).Directory;

        directoryInfo.ReceivedCalls().Should().HaveCount(1);
        directoryInfo.Received().EnumerateFiles("*.cs", SearchOption.AllDirectories);

        file.ReceivedCalls().Should().HaveCount(3);
        file.Received().Exists(OutputDllPath);
        file.Received().GetLastWriteTime(OutputDllPath);
        file.Received().Delete(OutputDllPath);

        monoCompiler.ReceivedCalls().Should().HaveCount(1);
        monoCompiler.Received().InvokeCompiler(Arg.Any<string[]>(), Arg.Any<TextWriter>());

        logger.ReceivedCalls().Should().HaveCount(3);
        logger.Received().Information("Compiling mod {identifier} ...", _ModDefinition.Identifier);
        logger.Received().Debug("outputDllPath: {outputDllPath}, Sources: {sources}, references: {references}", OutputDllPath, Arg.Any<string[]>(), Arg.Any<string[]>());
        logger.Received().Error("Compilation failed with error(s):\r\n{errors}", "ERROR");
    }

    [Fact]
    public void CompileMod_WhenOutdated_AndCompilationSucceed() {
        // Arrange
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.LastWriteTime.Returns(_NewDate);
        fileInfo.FullName.Returns(SourceFilePath);

        var directoryInfo = Substitute.For<IDirectoryInfo>();
        directoryInfo.EnumerateFiles(Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([fileInfo]);

        var file = Substitute.For<IFile>();
        file.Exists(Arg.Any<string>()).Returns(false);
        file.Exists(OutputDllPath).Returns(true);
        file.GetLastWriteTime(OutputDllPath).Returns(_OldDate);

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.DirectoryInfo(Arg.Any<string>()).Returns(directoryInfo);
        fileSystem.File.Returns(file);

        var monoCompiler = Substitute.For<ICompilerCallableEntryPoint>();
        monoCompiler.InvokeCompiler(Arg.Any<string[]>(), Arg.Any<TextWriter>()).Returns(true).AndDoes(_ => { file.Exists(OutputDllPath).Returns(true); });
        var logger = Substitute.For<ILogger>();
        var sut    = new CodeCompiler(fileSystem, monoCompiler, logger);

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(OutputDllPath);

        fileSystem.ReceivedCalls().Should().HaveCount(8);
        fileSystem.Received().DirectoryInfo(_ModDefinition.BasePath);
        _ = fileSystem.Received(3).File;
        _ = fileSystem.Received(4).Directory;

        directoryInfo.ReceivedCalls().Should().HaveCount(1);
        directoryInfo.Received().EnumerateFiles("*.cs", SearchOption.AllDirectories);

        file.ReceivedCalls().Should().HaveCount(3);
        file.Received().Exists(OutputDllPath);
        file.Received().GetLastWriteTime(OutputDllPath);
        file.Received().Delete(OutputDllPath);

        monoCompiler.ReceivedCalls().Should().HaveCount(1);
        monoCompiler.Received().InvokeCompiler(Arg.Any<string[]>(), Arg.Any<TextWriter>());

        logger.ReceivedCalls().Should().HaveCount(3);
        logger.Received().Information("Compiling mod {identifier} ...", _ModDefinition.Identifier);
        logger.Received().Debug("outputDllPath: {outputDllPath}, Sources: {sources}, references: {references}", OutputDllPath, Arg.Any<string[]>(), Arg.Any<string[]>());
        logger.Received().Information("Compilation complete ...");
    }

    [Fact]
    public void CompileMod_CallCompilerWithCorrectParameters() {
        // Arrange
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.LastWriteTime.Returns(_NewDate);
        fileInfo.FullName.Returns(SourceFilePath);

        var directoryInfo = Substitute.For<IDirectoryInfo>();
        directoryInfo.EnumerateFiles(Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([fileInfo]);

        var file = Substitute.For<IFile>();
        file.Exists(Arg.Any<string>()).Returns(false);
        file.Exists(OutputDllPath).Returns(true);
        file.GetLastWriteTime(OutputDllPath).Returns(_OldDate);

        var directory = Substitute.For<IDirectory>();
        directory.GetCurrentDirectory().Returns("CurrentDirectory");

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.DirectoryInfo(Arg.Any<string>()).Returns(directoryInfo);
        fileSystem.File.Returns(file);
        fileSystem.Directory.Returns(directory);

        var monoCompiler = Substitute.For<ICompilerCallableEntryPoint>();
        monoCompiler.InvokeCompiler(Arg.Any<string[]>(), Arg.Any<TextWriter>()).Returns(true).AndDoes(_ => { file.Exists(OutputDllPath).Returns(true); });
        var logger = Substitute.For<ILogger>();
        var sut = new CodeCompiler(fileSystem, monoCompiler, logger) {
            ReferenceNames = ["Foo", "Bar"]
        };

        // Act
        var actual = sut.CompileMod(_ModDefinition);

        // Assert
        actual.Should().Be(OutputDllPath);

        monoCompiler.Received().InvokeCompiler(Arg.Is<string[]>(o =>
            o.Length == 8 &&
            o[0] == SourceFilePath &&
            o[1] == "-target:library" &&
            o[2] == "-platform:anycpu" &&
            o[3] == $"-out:{OutputDllPath}" &&
            o[4] == "-optimize" &&
            o[5] == "-fullpaths" &&
            o[6] == "-warn:4" &&
            o[7] == @"-reference:CurrentDirectory\Railroader_Data\Managed\Foo.dll,CurrentDirectory\Railroader_Data\Managed\Bar.dll"
        ), Arg.Any<TextWriter>());

        logger.Received().Debug("outputDllPath: {outputDllPath}, Sources: {sources}, references: {references}", OutputDllPath,
            Arg.Is<string[]>(o => o.Length == 1 && o[0] == SourceFilePath),
            Arg.Is<string[]>(o =>
                o.Length == 2 &&
                o[0] == @"CurrentDirectory\Railroader_Data\Managed\Foo.dll" &&
                o[1] == @"CurrentDirectory\Railroader_Data\Managed\Bar.dll"));
    }


    [Fact]
    public void RealCode() {
        // Arrange
        const string  basePath = @"c:\Program Files (x86)\Steam\steamapps\common\Railroader\";
        Directory.SetCurrentDirectory(basePath);

        if (File.Exists(basePath + @"Mods\Railroader-DummyMod\DummyMod.dll")) {
            File.Delete(basePath + @"Mods\Railroader-DummyMod\DummyMod.dll");
        }

        var logger                     = Substitute.For<ILogger>();
        var fileSystem                 = new FileSystemWrapper(logger);
        var compilerCallableEntryPoint = new CompilerCallableEntryPointWrapper();
        var sut                        = new CodeCompiler(fileSystem, compilerCallableEntryPoint, logger);

        

        var modDefinition = new ModDefinition() {
            Identifier = "DummyMod",
            BasePath = basePath + @"Mods\Railroader-DummyMod\"
        };

        // Act
        var actual = sut.CompileMod(modDefinition);

        // Assert
    }
}
