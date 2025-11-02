using System.Collections.Generic;
using Railroader.ModManager.Extensions;
using Shouldly;

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
        key.ShouldBe(42);
        value.ShouldBe("answer");
    }
}
