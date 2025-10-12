using FluentAssertions;
using Railroader.ModInjector;
using Railroader.ModInterfaces;
using Xunit.Abstractions;

namespace Railroader_ModInterfaces.Tests;

[Collection("TestFixture")]
public sealed class CodeCompilerTests(TestFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public void TestCodeCompiler() {
        // Arrange
        var path          = fixture.GameDir + @"Mods\Railroader-DummyMod\";
        var outputDllPath = path + "DummyMod.dll";

        var sut = new CodeCompiler();
        IModDefinition definition = new ModDefinition {
            Id = "DummyMod",
            Name = "dummy name",
            DefinitionPath = path
        };

        // Act
        var actual = sut.CompileMod(definition);

        // Assert
        actual.Should().Be(outputDllPath);
        output.WriteLine(fixture.LogMessages);
    }
}
