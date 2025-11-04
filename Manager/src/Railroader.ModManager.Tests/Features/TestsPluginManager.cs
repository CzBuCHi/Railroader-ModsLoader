using System.Linq;
using NSubstitute;
using Railroader.ModManager.Delegates.System.Reflection.Assembly;
using Railroader.ModManager.Features;
using Railroader.ModManager.Interfaces;
using Serilog;
using Shouldly;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsPluginManager {
    private const string AssemblyPath = @"Mod\Dummy\Dummy.dll";

    private static Mod CreateMod(ILogger logger) {
        var modDefinition = Substitute.For<IModDefinition>();
        modDefinition.Identifier.Returns("Identifier");
        return new(logger, modDefinition) { AssemblyPath = AssemblyPath };
    }

    [Fact]
    public void CreatePlugins_WhenAssemblyFailsToLoad() {
        // Arrange
        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        var mod            = CreateMod(logger);

        // Act
        var plugins = PluginManager.LoadPlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.ShouldBeEmpty();
        logger.Received().Warning("Failed to load assembly from path: {AssemblyPath} for mod {ModId}", @"Mod\Dummy\Dummy.dll", "Identifier");
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
        var mod = CreateMod(logger);

        // Act
        var plugins = PluginManager.LoadPlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.ShouldBeEmpty();
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
        var mod = new Mod(logger, Substitute.For<IModDefinition>()) {
            AssemblyPath = AssemblyPath
        };

        // Act
        var plugins = PluginManager.LoadPlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.ShouldBeEmpty();
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
                
                public Foo(IModdingContext moddingContext, IMod mod) {
                }
            }
            """;

        var assembly = TestUtils.BuildAssembly(source, [typeof(TestsPluginManager).Assembly.GetName().Name]);

        var logger         = Substitute.For<ILogger>();
        var moddingContext = Substitute.For<IModdingContext>();
        var loadFrom       = Substitute.For<LoadFrom>();
        loadFrom.Invoke(Arg.Any<string>()).Returns(assembly);
        var mod = CreateMod(logger);

        // Act
        var plugins = PluginManager.LoadPlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.ShouldBeEmpty();
        logger.Received().Warning("Type {Type} implements IPlugin but does not inherit from PluginBase<> in mod {ModId}", "Foo", "Identifier");
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
        var mod = CreateMod(logger);

        // Act
        var plugins = PluginManager.LoadPlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.ShouldBeEmpty();
        logger.Received().Warning("Cannot find constructor (IModdingContext, IMod) on plugin {Plugin} in mod {ModId}", "TestPlugin", "Identifier");
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
        var mod = CreateMod(logger);

        // Act
        var plugins = PluginManager.LoadPlugins(moddingContext, logger, loadFrom, mod);

        // Assert
        plugins.Select(o => o.GetType().FullName).ToArray().ShouldBeEquivalentTo(new[] { "Foo.Bar.FirstPlugin", "Foo.Bar.SecondPlugin" });
    }
}