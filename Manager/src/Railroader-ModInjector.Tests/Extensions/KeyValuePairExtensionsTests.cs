using System.Collections.Generic;
using FluentAssertions;
using Railroader.ModManager.Extensions;

namespace Railroader.ModManager.Tests.Extensions;

public sealed class KeyValuePairExtensionsTests
{
    [Fact]
    public void Deconstruct() {
        // Arrange
        var sut = new KeyValuePair<int, string>(42, "answer");

        // Act
        var (key, value) = sut;

        // Assert
        key.Should().Be(42);
        value.Should().Be("answer");
    }
}
