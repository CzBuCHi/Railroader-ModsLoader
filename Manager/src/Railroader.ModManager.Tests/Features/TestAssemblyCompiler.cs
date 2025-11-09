using System.IO;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Delegates.Mono.CSharp.CompilerCallableEntryPoint;
using Railroader.ModManager.Features;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestAssemblyCompiler
{
    [Fact]
    public void CompileAssemblyWhenNoSourcesProvided() {
        // Arrange
        var invokeCompiler = Substitute.For<InvokeCompiler>();
        var logger         = Substitute.For<ILogger>();

        // Act
        var actual = AssemblyCompiler.Compile(invokeCompiler, logger, "outputPath", [], [], []);

        // Assert
        actual.ShouldBeFalse();

        logger.Received().Error("No source files provided for assembly compilation at {outputPath}.", "outputPath");

        invokeCompiler.DidNotReceive().Invoke(Arg.Any<string[]>(), Arg.Any<TextWriter>());
    }

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
        var actual = AssemblyCompiler.Compile(invokeCompiler, logger, "outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"],
            ["-resource:image.png,image"]
        );

        // Assert
        actual.ShouldBeTrue();

        logger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        logger.Received().Debug("References:\n{references}", "reference1.dll\nreference2.dll");
        logger.Received().Debug("Sources:\n{sources}", "source1.cs\nsource2.cs");
        logger.Received().Debug("Resources:\n{sources}", "-resource:image.png,image");
        logger.Received().Write(Arg.Any<LogEventLevel>(), "Compilation messages:\r\n{messages}", "Warning1\r\nWarning2");
        logger.Received().Information("Assembly {outputPath} compiled successfully", "outputPath");

        invokeCompiler.Received().Invoke(Arg.Is<string[]>(o => o.SequenceEqual(new[] {
            "source1.cs",
            "source2.cs",
            "-debug-",
            "-fullpaths",
            "-optimize",
            "-out:outputPath",
            "-reference:reference1.dll,reference2.dll",
            "-target:library",
            "-warn:4",
            "-resource:image.png,image"
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
        var actual = AssemblyCompiler.Compile(invokeCompiler, logger, "outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"], []);

        // Assert
        actual.ShouldBeFalse();

        logger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        logger.Received().Debug("References:\n{references}", "reference1.dll\nreference2.dll");
        logger.Received().Debug("Sources:\n{sources}", "source1.cs\nsource2.cs");
        logger.Received().Write(Arg.Any<LogEventLevel>(), "Compilation messages:\r\n{messages}", "Error1\r\nError2");
        logger.Received().Error("Compilation of assembly {outputPath} failed", "outputPath");


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
