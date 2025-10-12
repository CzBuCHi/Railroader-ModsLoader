using FluentAssertions;
using Railroader.ModInjector;
using Railroader.ModInterfaces;

namespace Railroader_ModInterfaces.Tests;

[Collection("TestFixture")]
public sealed class CodeCompilerTests(TestFixture fixture)
{
    [Fact]
    public void TestCodeCompiler() {
        // Arrange
        var path          = fixture.GameDir + @"Mods\Dummy\";
        var outputDllPath = path + "dummy.dll";

        var sut = new CodeCompiler();
        IModDefinition definition = new ModDefinition {
            Id = "dummy",
            Name = "dummy name",
            DefinitionPath = path
        };

        // Act
        var actual = sut.CompileMod(definition);

        // Assert
        actual.Should().Be(outputDllPath);
    }
}
