//using System.Reflection;
//using FluentAssertions;
//using Railroader.ModManager.Interfaces;
//using Railroader.ModManager.Services;

//namespace Railroader.ModManager.Tests.Services;

//public class TestsPluginManagerFactory
//{
//    [Fact]
//    public void CreateInstanceBinder() {
//        // Arrange
//        var serviceManager = new TestServiceManager();
//        var sut            = serviceManager.CreatePluginManagerFactory();

//        // Act
//        var actual = sut.CreatePluginManager(serviceManager.GetService<IModdingContext>());

//        // Assert
//        actual.Should().BeOfType<PluginManager>();
//        typeof(PluginManager).GetField("<loadAssemblyFrom>P", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actual).Should().Be(serviceManager.LoadAssemblyFrom);
//        typeof(PluginManager).GetField("<moddingContext>P", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actual).Should().Be(serviceManager.GetService<IModdingContext>());
//        typeof(PluginManager).GetField("<logger>P", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actual).Should().Be(serviceManager.MainLogger);
//    }
//}
