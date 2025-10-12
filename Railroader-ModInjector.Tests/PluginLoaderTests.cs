using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector;
using Railroader.ModInterfaces;

namespace Railroader_ModInterfaces.Tests;

[Collection("TestFixture")]
public sealed class PluginLoaderTests(TestFixture fixture)
{
    [Fact]
    public void LoadsCorrectPlugins() {
        // Arrange
        var outputDllPath  = fixture.GameDir + @"Mods\Dummy\dummy.dll";
        var moddingContext = Substitute.For<IModdingContext>();
        var sut            = new PluginLoader();

        // Act
        var plugins = sut.LoadPlugins(outputDllPath, moddingContext).ToArray();

        // Assert
        plugins.Should().HaveCount(1);
        plugins[0].Should().NotBeNull();
        plugins[0].GetType().FullName.Should().Be("Railroader.DummyMod.DummyPlugin");
    }
}
