using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector;
using Railroader.ModInterfaces;

namespace Railroader_ModInterfaces.Tests;

public class ModdingContextTests
{
    [Fact]
    public void ModsProperty() {
        // Arrange
        IReadOnlyCollection<IMod> mods = [Substitute.For<IMod>()];

        // Act
        var sut = new ModdingContext(mods);

        // Assert
        sut.Mods.Should().BeEquivalentTo(mods, o => o.WithStrictOrdering());
    }
}
