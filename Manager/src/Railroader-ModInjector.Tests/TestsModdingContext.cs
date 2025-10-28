using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Interfaces;

namespace Railroader.ModManager.Tests;

public sealed class TestsModdingContext
{
    [Fact]
    public void ModsProperty() {
        // Arrange
        IReadOnlyCollection<IMod> mods = [Substitute.For<IMod>()];

        // Act
        var sut = new ModdingContext(mods);

        // Assert
        sut.Mods.Should().BeEquivalentTo(mods);
    }
}
