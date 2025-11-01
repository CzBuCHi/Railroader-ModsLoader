using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Delegates.System.Reflection.Assembly;
using Railroader.ModManager.Features;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsPluginManager
{
    [Fact]
    public void CreatePlugins_WhenAssemblyFailsToLoad() {
        // Arrange
        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        var mod            = new Mod(logger, Substitute.For<IModDefinition>());

        // Act
        var plugins = PluginManager.CreatePlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreAbstractClasses() {
        // Arrange
        const string source =
            """
            using Railroader.ModManager.Interfaces;

            public abstract class Foo  {};

            public abstract class Bar : PluginBase<Bar> {
                public Bar(IModdingContext moddingContext, IMod mod) 
                    : base(moddingContext, mod) {
                }
            };
            """;

        var assembly = TestUtils.BuildAssembly(source, [typeof(TestsPluginManager).Assembly.GetName().Name]);

        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        loadFrom.Invoke(Arg.Any<string>()).Returns(assembly);
        var mod = new Mod(logger, Substitute.For<IModDefinition>());

        // Act
        var plugins = PluginManager.CreatePlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreClassesNotDerivedFromPluginBase() {
        // Arrange
        const string source =
            """
            public class Foo {
            }
            """;

        var assembly = TestUtils.BuildAssembly(source, [typeof(TestsPluginManager).Assembly.GetName().Name]);

        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        loadFrom.Invoke(Arg.Any<string>()).Returns(assembly);
        var mod = new Mod(logger, Substitute.For<IModDefinition>());

        // Act
        var plugins = PluginManager.CreatePlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.Should().BeEmpty();
    }

    [Fact]
    public void CreatePlugins_IgnoreClassesNotDerivedFromPluginBase_AndWarnIfIPluginBaseIsImplemented() {
        // Arrange
        const string source =
            """
            using Railroader.ModManager.Interfaces;

            public class Foo : IPlugin {
                public IModdingContext ModdingContext { get; }
                public IMod Mod { get; }
                public bool IsEnabled { get; set; }
            }
            """;

        var assembly = TestUtils.BuildAssembly(source, [typeof(TestsPluginManager).Assembly.GetName().Name]);

        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        loadFrom.Invoke(Arg.Any<string>()).Returns(assembly);
        var mod = new Mod(logger, Substitute.For<IModDefinition>());

        // Act
        var plugins = PluginManager.CreatePlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.Should().BeEmpty();

        logger.Received().Warning("Type {type} inherits IPluginBase but not PluginBase<> in mod {ModId}", Arg.Is<Type>(o => o.Name == "Foo"), mod.Definition.Identifier);
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
        var assembly = TestUtils.BuildAssembly(source, [typeof(TestsPluginManager).Assembly.GetName().Name]);

        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        loadFrom.Invoke(Arg.Any<string>()).Returns(assembly);
        var mod = new Mod(logger, Substitute.For<IModDefinition>());

        // Act
        var plugins = PluginManager.CreatePlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.Should().BeEmpty();

        logger.Received().Warning("Cannot find constructor that accepts IModdingContext, IMod parameters on plugin {plugin} in mod {ModId}", Arg.Is<Type>(o => o.Name == "TestPlugin"), mod.Definition.Identifier);
    }

    [Fact]
    public void CreatePlugins_ReturnValidInstances() {
        // Arrange
        const string source =
            """
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

        var assembly = TestUtils.BuildAssembly(source, [typeof(TestsPluginManager).Assembly.GetName().Name]);

        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        loadFrom.Invoke(Arg.Any<string>()).Returns(assembly);
        var mod = new Mod(logger, Substitute.For<IModDefinition>());

        // Act
        var plugins = PluginManager.CreatePlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.Select(o => o.GetType().FullName).Should().BeEquivalentTo("Foo.Bar.FirstPlugin", "Foo.Bar.SecondPlugin");
    }
}
