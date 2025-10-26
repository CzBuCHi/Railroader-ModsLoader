using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.Tests.Services;

public sealed class TestsPluginManager
{
    [Fact]
    public void CreatePlugins_WhenAssemblyFailsToLoad() {
        // Arrange
        var moddingContext  = Substitute.For<IModdingContext>();
        var assemblyWrapper = Substitute.For<IAssemblyWrapper>();
        var modDefinition   = Substitute.For<IModDefinition>();
        var logger          = Substitute.For<ILogger>();

        assemblyWrapper.LoadFrom(Arg.Any<string>()).Returns(_ => null);

        var mod = new Mod(modDefinition, "assemblyPath");
        var sut = new PluginManager {
            ModdingContext = moddingContext,
            AssemblyWrapper = assemblyWrapper,
            Logger = logger,
        };

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreAbstractClasses() {
        // Arrange
        var moddingContext  = Substitute.For<IModdingContext>();
        var assemblyWrapper = Substitute.For<IAssemblyWrapper>();
        var modDefinition   = Substitute.For<IModDefinition>();
        var logger          = Substitute.For<ILogger>();
        assemblyWrapper.LoadFrom(Arg.Any<string>()).Returns(AssemblyTestUtils.BuildAssembly(
            """
            using Railroader.ModInterfaces;
            
            public abstract class Foo  {};
            
            public abstract class Bar : PluginBase<Bar> {
                public Bar(IModdingContext moddingContext, IMod mod) 
                    : base(moddingContext, mod) {
                }
            };
            """
        ));
        
        var mod = new Mod(modDefinition, "assemblyPath");
        var sut = new PluginManager {
            ModdingContext = moddingContext,
            AssemblyWrapper = assemblyWrapper,
            Logger = logger
        };

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();
        logger.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreClassesNotDerivedFromPluginBase() {
        // Arrange
        var moddingContext  = Substitute.For<IModdingContext>();
        var assemblyWrapper = Substitute.For<IAssemblyWrapper>();
        var modDefinition   = Substitute.For<IModDefinition>();
        var logger          = Substitute.For<ILogger>();
        assemblyWrapper.LoadFrom(Arg.Any<string>()).Returns(AssemblyTestUtils.BuildAssembly(
            """
            public class Foo {
            }
            """
        ));
        
        var mod = new Mod(modDefinition, "assemblyPath");
        var sut = new PluginManager {
            ModdingContext = moddingContext,
            AssemblyWrapper = assemblyWrapper,
            Logger = logger
        };

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();
        logger.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreClassesNotDerivedFromPluginBase_AndWarnIfIPluginBaseIsImplemented() {
        // Arrange
        var moddingContext  = Substitute.For<IModdingContext>();
        var assemblyWrapper = Substitute.For<IAssemblyWrapper>();
        var modDefinition   = Substitute.For<IModDefinition>();
        var logger          = Substitute.For<ILogger>();
        assemblyWrapper.LoadFrom(Arg.Any<string>()).Returns(AssemblyTestUtils.BuildAssembly(
            """
            using Railroader.ModInterfaces;
            
            public class Foo : IPluginBase {
                public IModdingContext ModdingContext { get; }
                public IMod Mod { get; }
                public bool IsEnabled { get; set; }
            }
            """
        ));
        
        var mod = new Mod(modDefinition, "assemblyPath");
        var sut = new PluginManager {
            ModdingContext = moddingContext,
            AssemblyWrapper = assemblyWrapper,
            Logger = logger
        };

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();

        logger.Received().Warning("Type {type} inherits IPluginBase but not PluginBase<> in mod {ModId}", Arg.Is<Type>(o => o.Name == "Foo"), mod.Definition.Identifier);
    }

    [Theory]
    [InlineData("""
                using Railroader.ModInterfaces;
                using Serilog;

                public sealed class TestPlugin : PluginBase<TestPlugin>
                {
                    public TestPlugin() 
                        : base(null, null) {
                    }
                }
                """)]
    [InlineData("""
                using Railroader.ModInterfaces;
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
        var moddingContext  = Substitute.For<IModdingContext>();
        var assemblyWrapper = Substitute.For<IAssemblyWrapper>();
        var modDefinition   = Substitute.For<IModDefinition>();
        var logger          = Substitute.For<ILogger>();

        var assembly = AssemblyTestUtils.BuildAssembly(source);
        assemblyWrapper.LoadFrom(Arg.Any<string>()).Returns(assembly);

        var mod = new Mod(modDefinition, "assemblyPath");
        var sut = new PluginManager {
            ModdingContext = moddingContext,
            AssemblyWrapper = assemblyWrapper,
            Logger = logger
        };

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Should().BeEmpty();

        logger.Received().Warning("Cannot find constructor that accepts IModdingContext, IMod parameters on plugin {plugin} in mod {ModId}", Arg.Is<Type>(o => o.Name == "TestPlugin"), mod.Definition.Identifier);

    }
    
    [Fact]
    public void CreatePlugins_ReturnValidInstances() {
        // Arrange
        const string source = """
                              using Railroader.ModInterfaces;
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

        var moddingContext  = Substitute.For<IModdingContext>();
        var assemblyWrapper = Substitute.For<IAssemblyWrapper>();
        var modDefinition   = Substitute.For<IModDefinition>();
        var logger          = Substitute.For<ILogger>();

        var assembly        = AssemblyTestUtils.BuildAssembly(source);
        assemblyWrapper.LoadFrom(Arg.Any<string>()).Returns(assembly);

        var mod = new Mod(modDefinition, "assemblyPath");
        var sut = new PluginManager {
            ModdingContext = moddingContext,
            AssemblyWrapper = assemblyWrapper,
            Logger = logger,
        };

        // Act
        var plugins = sut.CreatePlugins(mod);

        // Assert
        plugins.Select(o => o.GetType().FullName).Should().BeEquivalentTo("Foo.Bar.FirstPlugin", "Foo.Bar.SecondPlugin");
    }
}