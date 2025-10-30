using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Delegates.Mono.CSharp.CompilerCallableEntryPoint;
using Railroader.ModManager.Features;
using Serilog;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsCompileAssemblyCore
{
    [Fact]
    public void CompileAssemblyWhenSuccessful() {
        // Arrange
        var invokeCompiler = Substitute.For<InvokeCompiler>();
        invokeCompiler.Invoke(Arg.Any<string[]>(), Arg.Any<TextWriter>()).Returns(callInfo => {
            callInfo.Arg<TextWriter>().Write("Warning1\r\nWarning2");
            return true;
        });

        var logger = Substitute.For<ILogger>();

        // Act
        var actual = CompileAssembly.Execute(invokeCompiler, logger, "outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"], out var messages);

        // Assert
        actual.Should().BeTrue();

        messages.Should().Be("Warning1\r\nWarning2");

        logger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        logger.Received().Debug("reference: {source}", "reference1.dll");
        logger.Received().Debug("reference: {source}", "reference2.dll");
        logger.Received().Debug("source: {source}", "source1.cs");
        logger.Received().Debug("source: {source}", "source2.cs");
        logger.Received().Information("Compilation messages:\r\n{messages}", "Warning1\r\nWarning2");
        logger.Received().Information("Assembly {outputPath} compiled successfully", "outputPath");
        logger.ReceivedCalls().Should().HaveCount(7);

        invokeCompiler.Received().Invoke(Arg.Is<string[]>(o => o.SequenceEqual(new[] {
            "source1.cs",
            "source2.cs",
            "-debug-",
            "-fullpaths",
            "-optimize",
            "-out:outputPath",
            "-reference:reference1.dll,reference2.dll",
            "-target:library",
            "-warn:4"
        })), Arg.Any<TextWriter>());
    }

    [Fact]
    public void CompileAssemblyWhenFailed() {
        // Arrange
        var invokeCompiler = Substitute.For<InvokeCompiler>();
        invokeCompiler.Invoke(Arg.Any<string[]>(), Arg.Any<TextWriter>()).Returns(callInfo => {
            callInfo.Arg<TextWriter>().Write("Error1\r\nError2");
            return false;
        });

        var logger = Substitute.For<ILogger>();

        // Act
        var actual = CompileAssembly.Execute(invokeCompiler, logger, "outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"], out var messages);

        // Assert
        actual.Should().BeFalse();

        messages.Should().Be("Error1\r\nError2");

        logger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        logger.Received().Debug("reference: {source}", "reference1.dll");
        logger.Received().Debug("reference: {source}", "reference2.dll");
        logger.Received().Debug("source: {source}", "source1.cs");
        logger.Received().Debug("source: {source}", "source2.cs");
        logger.Received().Information("Compilation messages:\r\n{messages}", "Error1\r\nError2");
        logger.Received().Error("Compilation of assembly {outputPath} failed", "outputPath");
        logger.ReceivedCalls().Should().HaveCount(7);

        invokeCompiler.Received().Invoke(Arg.Is<string[]>(o => o.SequenceEqual(new[] {
            "source1.cs",
            "source2.cs",
            "-debug-",
            "-fullpaths",
            "-optimize",
            "-out:outputPath",
            "-reference:reference1.dll,reference2.dll",
            "-target:library",
            "-warn:4"
        })), Arg.Any<TextWriter>());
    }
}
