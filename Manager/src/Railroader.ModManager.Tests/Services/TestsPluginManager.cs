using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Interfaces;

namespace Railroader.ModManager.Tests.Services;

public sealed class TestsPluginManager
{
    [Fact]
    public void CreatePlugins_WhenAssemblyFailsToLoad() {
        // Arrange
        var serviceManager = new TestServiceManager();

        var mod = new Mod(Substitute.For<IModDefinition>(), "assemblyPath");
        var sut = serviceManager.CreatePluginManager();

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreAbstractClasses() {
        // Arrange
        var serviceManager =
            new TestServiceManager()
                .WithAssembly("assemblyPath",
                    """
                    using Railroader.ModManager.Interfaces;

                    public abstract class Foo  {};

                    public abstract class Bar : PluginBase<Bar> {
                        public Bar(IModdingContext moddingContext, IMod mod) 
                            : base(moddingContext, mod) {
                        }
                    };
                    """);

        var mod = new Mod(Substitute.For<IModDefinition>(), "assemblyPath");
        var sut = serviceManager.CreatePluginManager();

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();
        serviceManager.MainLogger.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreClassesNotDerivedFromPluginBase() {
        // Arrange
        var serviceManager =
            new TestServiceManager()
                .WithAssembly("assemblyPath",
                    """
                    public class Foo {
                    }
                    """);

        var mod = new Mod(Substitute.For<IModDefinition>(), "assemblyPath");
        var sut = serviceManager.CreatePluginManager();

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();
        serviceManager.MainLogger.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreClassesNotDerivedFromPluginBase_AndWarnIfIPluginBaseIsImplemented() {
        // Arrange
        var serviceManager =
            new TestServiceManager()
                .WithAssembly("assemblyPath",
                    """
                    using Railroader.ModManager.Interfaces;

                    public class Foo : IPlugin {
                        public IModdingContext ModdingContext { get; }
                        public IMod Mod { get; }
                        public bool IsEnabled { get; set; }
                    }
                    """);

        var mod = new Mod(Substitute.For<IModDefinition>(), "assemblyPath");
        var sut = serviceManager.CreatePluginManager();

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();

        serviceManager.MainLogger.Received().Warning("Type {type} inherits IPluginBase but not PluginBase<> in mod {ModId}", Arg.Is<Type>(o => o.Name == "Foo"), mod.Definition.Identifier);
    }

    [Theory]
    [InlineData("""
                using Railroader.ModManager.Interfaces;
                using Serilog;

                public sealed class TestPlugin : PluginBase<TestPlugin>
                {
                    public TestPlugin() 
                        : base(null, null) {
                    }
                }
                """)]
    [InlineData("""
                using Railroader.ModManager.Interfaces;
                using Serilog;

                public sealed class TestPlugin : PluginBase<TestPlugin>
                {
                    public TestPlugin(IModdingContext moddingContext, IMod mod, int extra) 
                        : base(moddingContext, mod) {
                    }
                }
                """)]
    public void CreatePlugins_IgnorePluginsWithInvalidConstructor(string source) {
        // Arrange
        var serviceManager =
            new TestServiceManager()
                .WithAssembly("assemblyPath", source);

        var mod = new Mod(Substitute.For<IModDefinition>(), "assemblyPath");
        var sut = serviceManager.CreatePluginManager();

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();

        serviceManager.MainLogger.Received().Warning("Cannot find constructor that accepts IModdingContext, IMod parameters on plugin {plugin} in mod {ModId}", Arg.Is<Type>(o => o.Name == "TestPlugin"), mod.Definition.Identifier);
    }

    [Fact]
    public void CreatePlugins_ReturnValidInstances() {
        // Arrange
        const string source = """
                              using Railroader.ModManager.Interfaces;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                                  
                                  public sealed class SecondPlugin : PluginBase<SecondPlugin>
                                  {
                                      public SecondPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                              }
                              """;

        var serviceManager =
            new TestServiceManager()
                .WithAssembly("assemblyPath", source);

        var mod = new Mod(Substitute.For<IModDefinition>(), "assemblyPath");
        var sut = serviceManager.CreatePluginManager();

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Select(o => o.GetType().FullName).Should().BeEquivalentTo("Foo.Bar.FirstPlugin", "Foo.Bar.SecondPlugin");
    }
}
