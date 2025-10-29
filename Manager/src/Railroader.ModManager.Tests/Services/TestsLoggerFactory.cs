//using FluentAssertions;
//using NSubstitute;

//namespace Railroader.ModManager.Tests.Services;

//public class TestsLoggerFactory
//{
//    [Theory]
//    [InlineData(null!)]
//    [InlineData("scope")]
//    public void CreateLogger(string? scope) {
//        // Arrange
//        var serviceManager = new TestServiceManager();
//        var sut            = serviceManager.CreateLoggerFactory();

//        // Act
//        var actual = sut.GetLogger(scope);


//        // Assert
//        serviceManager.ContextLogger.Should().Be(actual);

//        serviceManager.MainLogger.Received().ForContext("SourceContext", scope ?? "Railroader.ModInjector");
//    }
//}
