using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Tests;

public sealed class TestsModdingContext
{
    [Fact]
    public void ModsProperty() {
        // Arrange
        IReadOnlyCollection<IMod> mods   = [Substitute.For<IMod>()];
        var                       logger = Substitute.For<ILogger>();

        // Act
        var sut = new ModdingContext(mods, logger);

        // Assert
        sut.Mods.Should().BeEquivalentTo(mods);
    }
}
