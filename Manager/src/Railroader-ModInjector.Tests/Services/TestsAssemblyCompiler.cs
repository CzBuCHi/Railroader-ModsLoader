using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using ILogger = Serilog.ILogger;

namespace Railroader.ModInjector.Tests.Services;

public sealed class TestsAssemblyCompiler
{
    [Fact]
    public void CompileAssemblyWhenSuccessful() {
        // Arrange
        var compilerCallableEntryPoint = Substitute.For<ICompilerCallableEntryPoint>();
        compilerCallableEntryPoint.InvokeCompiler(Arg.Any<string[]>(), Arg.Is<TextWriter>(_ => true)).Returns(o => {
            var writer = o.ArgAt<TextWriter>(1);
            writer.Write("Warning1\r\nWarning2\r\n");
            return true;
        });

        var logger = Substitute.For<ILogger>();

        var sut = new AssemblyCompiler {
            CompilerCallableEntryPoint = compilerCallableEntryPoint,
            Logger = logger
        };

        string[] expectedArgs = [
            "source1.cs",
            "source2.cs",
            "-debug-",
            "-fullpaths",
            "-optimize",
            "-out:outputPath",
            "-reference:reference1.dll,reference2.dll",
            "-target:library",
            "-warn:4"
        ];

        // Act
        var actual = sut.CompileAssembly("outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"]);

        // Assert
        actual.Should().BeTrue();
        logger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        logger.Received().Debug("reference: {source}", "reference1.dll");
        logger.Received().Debug("reference: {source}", "reference2.dll");
        logger.Received().Debug("source: {source}", "source1.cs");
        logger.Received().Debug("source: {source}", "source2.cs");
        logger.Received().Information("Compilation messages:\r\n{messages}", "Warning1\r\nWarning2\r\n");
        logger.Received().Information("Assembly {outputPath} compiled successfully", "outputPath");
        logger.ReceivedCalls().Should().HaveCount(7);

        compilerCallableEntryPoint.Received().InvokeCompiler(Arg.Is<string[]>(o => o.SequenceEqual(expectedArgs)), Arg.Any<TextWriter>());
    }

    [Fact]
    public void CompileAssemblyWhenFailed() {
        // Arrange
        var compilerCallableEntryPoint = Substitute.For<ICompilerCallableEntryPoint>();
        compilerCallableEntryPoint.InvokeCompiler(Arg.Any<string[]>(), Arg.Is<TextWriter>(_ => true)).Returns(o => {
            var writer = o.ArgAt<TextWriter>(1);
            writer.Write("Error1\r\nError2\r\n");
            return false;
        });

        var logger = Substitute.For<ILogger>();

        var sut = new AssemblyCompiler {
            CompilerCallableEntryPoint = compilerCallableEntryPoint,
            Logger = logger
        };

        // Act
        var actual = sut.CompileAssembly("outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"]);

        // Assert
        actual.Should().BeFalse();
        logger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        logger.Received().Debug("reference: {source}", "reference1.dll");
        logger.Received().Debug("reference: {source}", "reference2.dll");
        logger.Received().Debug("source: {source}", "source1.cs");
        logger.Received().Debug("source: {source}", "source2.cs");
        logger.Received().Information("Compilation messages:\r\n{messages}", "Error1\r\nError2\r\n");
        logger.Received().Error("Compilation of assembly {outputPath} failed", "outputPath");
        logger.ReceivedCalls().Should().HaveCount(7);
    }
}
