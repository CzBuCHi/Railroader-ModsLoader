using FluentAssertions;
using NSubstitute;
using Serilog;
using LoggerExtensions = Railroader.ModManager.Extensions.LoggerExtensions;

namespace Railroader.ModManager.Tests.Extensions;

public sealed class TestLoggerExtensions
{
    [Theory]
    [InlineData(null!)]
    [InlineData("Scope")]
    public void ForSourceContext(string? scope) {
        // Arrange
        var logger = Substitute.For<ILogger>();

        // Act
        var actual = LoggerExtensions.ForSourceContext(logger, scope);

        // Assert
        actual.Should().NotBeNull().And.NotBe(logger);

        logger.Received(1).ForContext("SourceContext", scope ?? "Railroader.ModManager");
        logger.ReceivedCalls().Should().HaveCount(1);
    }
}
