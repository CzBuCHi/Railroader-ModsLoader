using System;
using FluentAssertions;
using Railroader.ModInterfaces;

namespace Railroader_ModInterfaces.Tests;

public class FluentVersionTests
{
    [Theory]
    [InlineData("1.0", VersionOperator.Equal, "1.0")]
    [InlineData("1.0", VersionOperator.GreaterThan, ">1.0")]
    [InlineData("1.0", VersionOperator.GreaterOrEqual, ">=1.0")]
    [InlineData("1.0", VersionOperator.LessOrEqual, "<=1.0")]
    [InlineData("1.0", VersionOperator.LessThan, "<1.0")]
    [InlineData("1.0", (VersionOperator)(-1), "[Invalid operator: -1]1.0")]
    public void ToStringTest(string version, VersionOperator @operator, string expected) {
        // Arrange
        var sut = new FluentVersion(Version.Parse(version), @operator);

        // Act
        var actual = sut.ToString();

        // Assert
        actual.Should().Be(expected);
    }
}
