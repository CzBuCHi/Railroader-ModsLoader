//using System.Reflection;
//using FluentAssertions;
//using Railroader.ModManager.CodePatchers;
//using Railroader.ModManager.CodePatchers.Special;
//using Railroader.ModManager.Interfaces;

//namespace Railroader.ModManager.Tests.CodePatchers.Special;

//public sealed class TestsTopRightButtonPluginPatcher
//{
//    [Fact]
//    public void Constructor() {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        var methodPatchersField = typeof(TypePatcher).GetField("_MethodPatchers", BindingFlags.Instance | BindingFlags.NonPublic)!;

//        var type                = typeof(MethodPatcher<ITopRightButtonPlugin, TopRightButtonPluginPatcher>);
//        var targetBaseTypeField = type.GetField("_TargetBaseType", BindingFlags.Instance | BindingFlags.NonPublic)!;
//        var loggerField         = type.GetField("_Logger", BindingFlags.Instance | BindingFlags.NonPublic)!;
//        var targetMethodField   = type.GetField("_TargetMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
//        var injectedMethodField = type.GetField("_InjectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)!;
//        var injectedMethod      = typeof(TopRightButtonPluginPatcher).GetMethod("OnIsEnabledChanged", BindingFlags.Static | BindingFlags.Public)!;

//        // Act
//        var sut = serviceManager.CreateTopRightButtonPluginPatcher();

//        // Assert
//        var methodPatchers = methodPatchersField.GetValue(sut);
//        var array          = methodPatchers.Should().BeOfType<IMethodPatcher[]>().Which;
//        array.Should().HaveCount(1);
//        var patcher = array[0].Should().BeOfType<MethodPatcher<ITopRightButtonPlugin, TopRightButtonPluginPatcher>>().Which;

//        targetBaseTypeField.GetValue(patcher).Should().Be(typeof(PluginBase<>));
//        loggerField.GetValue(patcher).Should().Be(serviceManager.ContextLogger);
//        targetMethodField.GetValue(patcher).Should().Be("OnIsEnabledChanged");
//        injectedMethodField.GetValue(patcher).Should().Be(injectedMethod);
//    }
//}
