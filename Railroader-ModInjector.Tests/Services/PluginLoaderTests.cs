using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Mono.CSharp;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader_ModInterfaces.Tests.Services;

public sealed class PluginLoaderTests
{
    private const string AssemblyPath  = @"Mods\Dummy\dummy.dll";

    [Fact]
    public void LoadPlugins_ErrorWhenAssemblyNotLoaded() {
        // Arrange
        var assemblyLoader = Substitute.For<IAssemblyLoader>();
        assemblyLoader.Load(Arg.Any<string>()).Throws(new FileNotFoundException());
        var moddingContext = Substitute.For<IModdingContext>();
        var logger         = Substitute.For<ILogger>();
        var sut            = new PluginLoader(assemblyLoader, logger);

        // Act
        var plugins = sut.LoadPlugins(AssemblyPath, moddingContext).ToArray();

        // Assert
        plugins.Should().HaveCount(0);

        assemblyLoader.ReceivedCalls().Should().HaveCount(1);
        assemblyLoader.Received().Load(AssemblyPath);

        moddingContext.ReceivedCalls().Should().HaveCount(0);

        logger.ReceivedCalls().Should().HaveCount(2);
        logger.Received().Information("Loading assembly from {assemblyPath} ...", AssemblyPath);
        logger.Received().Error("Failed to load mod assembly from {assemblyPath}, error: {error}", AssemblyPath, Arg.Any<FileNotFoundException>());
    }

    [Fact]
    public void LoadPlugins_SkipNonPluginTypes() {
        // Arrange
        var assembly = BuildAssembly(@"
using Railroader.ModInterfaces;

namespace Foo { 
    class Bar { } 
    abstract class Baz : PluginBase {
        Baz(IModdingContext moddingContext) : base(moddingContext) {
        }
    }
    static class Fizz {}
}");

        var assemblyLoader = Substitute.For<IAssemblyLoader>();
        assemblyLoader.Load(Arg.Any<string>()).Returns(assembly);
        var moddingContext = Substitute.For<IModdingContext>();
        var logger         = Substitute.For<ILogger>();
        var sut            = new PluginLoader(assemblyLoader, logger);

        // Act
        var plugins = sut.LoadPlugins(AssemblyPath, moddingContext).ToArray();

        // Assert
        plugins.Should().HaveCount(0);
        assemblyLoader.ReceivedCalls().Should().HaveCount(1);
        assemblyLoader.Received().Load(AssemblyPath);

        moddingContext.ReceivedCalls().Should().HaveCount(0);

        logger.ReceivedCalls().Should().HaveCount(4);
        logger.Received().Information("Loading assembly from {assemblyPath} ...", AssemblyPath);
        logger.Received().Debug("Checking type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Debug("Checking type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Baz"));
        logger.Received().Debug("Checking type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Fizz"));
    }

    [Fact]
    public void LoadPlugins_VerifyConstructor() {
        // Arrange
        var assembly = BuildAssembly(@"
using Railroader.ModInterfaces;

namespace Foo { 
    class Bar : PluginBase {
        Bar() : base(null) {
        }
    }
    class Baz : PluginBase {
        Baz(IModdingContext context, int extra) : base(context) {
        }
    }
}");

        var assemblyLoader = Substitute.For<IAssemblyLoader>();
        assemblyLoader.Load(Arg.Any<string>()).Returns(assembly);
        var moddingContext = Substitute.For<IModdingContext>();
        var logger         = Substitute.For<ILogger>();
        var sut            = new PluginLoader(assemblyLoader, logger);

        // Act
        var plugins = sut.LoadPlugins(AssemblyPath, moddingContext).ToArray();

        // Assert
        plugins.Should().HaveCount(0);
        assemblyLoader.ReceivedCalls().Should().HaveCount(1);
        assemblyLoader.Received().Load(AssemblyPath);

        moddingContext.ReceivedCalls().Should().HaveCount(0);

        logger.ReceivedCalls().Should().HaveCount(7);
        logger.Received().Information("Loading assembly from {assemblyPath} ...", AssemblyPath);
        logger.Received().Debug("Checking type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Debug("Found PluginBase-derived type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Error("No constructor found in {type} that accepts IModdingContext", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Debug("Checking type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Baz"));
        logger.Received().Debug("Found PluginBase-derived type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Baz"));
        logger.Received().Error("No constructor found in {type} that accepts IModdingContext", Arg.Is<Type>(o => o.FullName == "Foo.Baz"));
    }

    [Fact]
    public void LoadPlugins_TryCreateInstance() {
        // Arrange
        var assembly = BuildAssembly(@"
using System;
using Railroader.ModInterfaces;

namespace Foo { 
    class Bar : PluginBase {
        Bar(IModdingContext context) : base(context) {
            throw new Exception(""Error"");
        }
    }
}");

        var assemblyLoader = Substitute.For<IAssemblyLoader>();
        assemblyLoader.Load(Arg.Any<string>()).Returns(assembly);
        var moddingContext = Substitute.For<IModdingContext>();
        var logger         = Substitute.For<ILogger>();
        var sut            = new PluginLoader(assemblyLoader, logger);

        // Act
        var plugins = sut.LoadPlugins(AssemblyPath, moddingContext).ToArray();

        // Assert
        plugins.Should().HaveCount(0);
        assemblyLoader.ReceivedCalls().Should().HaveCount(1);
        assemblyLoader.Received().Load(AssemblyPath);

        moddingContext.ReceivedCalls().Should().HaveCount(0);

        logger.ReceivedCalls().Should().HaveCount(4);
        logger.Received().Information("Loading assembly from {assemblyPath} ...", AssemblyPath);
        logger.Received().Debug("Checking type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Debug("Found PluginBase-derived type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Error("Failed to create {type}. error: {error}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"), Arg.Is<Exception>(o => o.Message == "Error"));
    }

    [Fact]
    public void LoadPlugins_CreateInstance() {
        // Arrange
        var assembly = BuildAssembly(@"
using System;
using Railroader.ModInterfaces;

namespace Foo { 
    class Bar : PluginBase {
        Bar(IModdingContext context) : base(context) {
        }
    }
}");

        var assemblyLoader = Substitute.For<IAssemblyLoader>();
        assemblyLoader.Load(Arg.Any<string>()).Returns(assembly);
        var moddingContext = Substitute.For<IModdingContext>();
        var logger         = Substitute.For<ILogger>();
        var sut            = new PluginLoader(assemblyLoader, logger);

        // Act
        var plugins = sut.LoadPlugins(AssemblyPath, moddingContext).ToArray();

        // Assert
        plugins.Should().HaveCount(1);
        plugins[0].GetType().FullName.Should().Be("Foo.Bar");

        assemblyLoader.ReceivedCalls().Should().HaveCount(1);
        assemblyLoader.Received().Load(AssemblyPath);

        moddingContext.ReceivedCalls().Should().HaveCount(0);

        logger.ReceivedCalls().Should().HaveCount(4);
        logger.Received().Information("Loading assembly from {assemblyPath} ...", AssemblyPath);
        logger.Received().Debug("Checking type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Debug("Found PluginBase-derived type: {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        logger.Received().Debug("Successfully created instance of {type}", Arg.Is<Type>(o => o.FullName == "Foo.Bar"));
        
    }

    private static Assembly BuildAssembly(string sourceCode) {
        var settings  = new CompilerSettings();
        var printer   = new SimplePrinter();
        var context   = new CompilerContext(settings, printer);
        var evaluator = new Evaluator(context);
        evaluator.ReferenceAssembly(typeof(PluginBase).Assembly);
        evaluator.Compile(sourceCode);

        printer.Messages.Should().BeEmpty();
        

        return (Assembly)evaluator.Evaluate("typeof(Foo.Bar).Assembly")!;
    }

    private sealed class SimplePrinter : ReportPrinter
    {
        public List<string> Messages { get; } = new();

        public override void Print(AbstractMessage msg, bool showFullPath) {
            base.Print(msg, showFullPath);

            var sb = new StringBuilder();
            using (TextWriter output = new StringWriter(sb)) {
                Print(msg, output, showFullPath);
            }

            Messages.Add(sb.ToString());

        }
    }
}
