using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Delegates;

namespace Railroader.ModManager.Tests.Services;

public sealed class TestsCompileAssemblyCore
{
    [Fact]
    public void CompileAssemblyWhenSuccessful() {
        // Arrange
        var serviceManager =
            new TestServiceManager()
                .WithCompilerCallableEntryPoint(true, "Warning1\r\nWarning2\r\n");
        
        var sut = serviceManager.CreateCompileAssembly();

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
        var actual = sut("outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"], out _);

        // Assert
        actual.Should().BeTrue();
        serviceManager.ContextLogger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        serviceManager.ContextLogger.Received().Debug("reference: {source}", "reference1.dll");
        serviceManager.ContextLogger.Received().Debug("reference: {source}", "reference2.dll");
        serviceManager.ContextLogger.Received().Debug("source: {source}", "source1.cs");
        serviceManager.ContextLogger.Received().Debug("source: {source}", "source2.cs");
        serviceManager.ContextLogger.Received().Information("Compilation messages:\r\n{messages}", "Warning1\r\nWarning2\r\n");
        serviceManager.ContextLogger.Received().Information("Assembly {outputPath} compiled successfully", "outputPath");
        serviceManager.ContextLogger.ReceivedCalls().Should().HaveCount(7);

        serviceManager.GetService<InvokeCompilerDelegate>().Received().Invoke(Arg.Is<string[]>(o => o.SequenceEqual(expectedArgs)), Arg.Any<TextWriter>());
    }

    [Fact]
    public void CompileAssemblyWhenFailed() {
        // Arrange
        var serviceManager =
            new TestServiceManager()
                .WithCompilerCallableEntryPoint(false, "Error1\r\nError2\r\n");


        var sut = serviceManager.CreateCompileAssembly();

        // Act
        var actual = sut("outputPath", ["source1.cs", "source2.cs"], ["reference1.dll", "reference2.dll"], out _);

        // Assert
        actual.Should().BeFalse();
        serviceManager.ContextLogger.Received().Information("Compiling assembly {outputPath} ...", "outputPath");
        serviceManager.ContextLogger.Received().Debug("reference: {source}", "reference1.dll");
        serviceManager.ContextLogger.Received().Debug("reference: {source}", "reference2.dll");
        serviceManager.ContextLogger.Received().Debug("source: {source}", "source1.cs");
        serviceManager.ContextLogger.Received().Debug("source: {source}", "source2.cs");
        serviceManager.ContextLogger.Received().Information("Compilation messages:\r\n{messages}", "Error1\r\nError2\r\n");
        serviceManager.ContextLogger.Received().Error("Compilation of assembly {outputPath} failed", "outputPath");
        serviceManager.ContextLogger.ReceivedCalls().Should().HaveCount(7);
    }
}
