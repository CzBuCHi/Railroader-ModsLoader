using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using Railroader.ModInjector.Patchers;
using Serilog;

namespace Railroader_ModInterfaces.Tests.Patchers;

public sealed class MethodPatcherTests
{
    [Theory]
    [InlineData("NotExistingInjectedMethod")]
    [InlineData("PrivateInjectedMethod")]
    [InlineData("PrivateStaticInjectedMethod")]
    [InlineData("PublicInjectedMethod")]
    public void ThrowInvalidInjectorMethod(string injectorMethod) {
        // Arrange
        var logger = Substitute.For<ILogger>();

        // Act
        var act = () => new MethodPatcher<IMarker, MethodPatcherTests>(logger, typeof(BaseType), "TargetMethod", injectorMethod);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Injected method must be public static void method on public class.*");
    }

    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
    private void PrivateInjectedMethod() { }

    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
    private static void PrivateStaticInjectedMethod() { }

    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
#pragma warning disable xUnit1013
    public void PublicInjectedMethod() { }
#pragma warning restore xUnit1013

    [Theory]
    [InlineData("1", """
                     namespace Foo.Bar { 
                        public class TargetType { }
                     }
                     """)]
    [InlineData("2", """
                     using Railroader_ModInterfaces.Tests.Patchers;
                     
                     namespace Foo.Bar { 
                        public class TargetType : IMarker { } 
                     }
                     """)]
    [InlineData("3", """
                     using Railroader_ModInterfaces.Tests.Patchers;
                     
                     namespace Foo.Bar { 
                        public class TargetType : BaseType { } 
                     }
                     """)]
    public void SkipNotMarkedTypes(string suffix, string source) {
        // Arrange
        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinitionX(source, suffix);
        var typeDefinition = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = new MethodPatcher<IMarker, Patcher>(logger, typeof(BaseType), "TargetMethod", "InjectedMethod");

        // Act
        var actual = sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Name!.Name = "patched";
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        actual.Should().BeFalse();

        logger.Received().Debug("Skipping patching for type {TypeName}: not derived from {BaseType} or does not implement {MarkerInterface}", typeDefinition.FullName, typeof(BaseType), typeof(IMarker));
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void ErrorIfTargetMethodNotExists() {
        // Arrange
        const string source = """
                              using Railroader_ModInterfaces.Tests.Patchers;
                              namespace Foo.Bar { 
                                  public class TargetType : BaseType, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinitionX(source);
        var typeDefinition = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = new MethodPatcher<IMarker, Patcher>(logger, typeof(BaseType), "InvalidTargetMethod", "InjectedMethod");

        // Act
        var actual = sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Name!.Name = "patched";
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        actual.Should().BeFalse();
        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "InvalidTargetMethod", typeDefinition.FullName);
        logger.Received().Error("Virtual method '{MethodName}' not found in {TypeName} hierarchy!", "InvalidTargetMethod", typeDefinition.FullName);
        logger.ReceivedCalls().Should().HaveCount(2);
    }

    [Fact]
    public void CreateOverrideIfNeeded() {
        // Arrange
        const string source = """
                              using Railroader_ModInterfaces.Tests.Patchers;
                              namespace Foo.Bar { 
                                  public class TargetType : BaseType, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinitionX(source);
        var typeDefinition = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = new MethodPatcher<IMarker, Patcher>(logger, typeof(BaseType), "TargetMethod", "InjectedMethod");

        // Act
        var first = sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Name!.Name = "patched";
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        first.Should().BeTrue();
        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "TargetMethod", typeDefinition.FullName);
        logger.Received().Debug("Created {MethodName} override with base call in {TypeName}", "TargetMethod", typeDefinition.FullName);
        logger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
        logger.ReceivedCalls().Should().HaveCount(3);
    }

    [Fact]
    public void SkipDuplicatePatch() {
        // Arrange
        const string source = """
                              using Railroader_ModInterfaces.Tests.Patchers;
                              namespace Foo.Bar { 
                                  public class TargetType : BaseType, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinitionX(source);
        var typeDefinition = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = new MethodPatcher<IMarker, Patcher>(logger, typeof(BaseType), "TargetMethod", "InjectedMethod");

        // Act
        var first = sut.Patch(assemblyDefinition, typeDefinition);
        var second = sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Name!.Name = "patched";
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "TargetMethod", typeDefinition.FullName);
        logger.Received().Debug("Created {MethodName} override with base call in {TypeName}", "TargetMethod", typeDefinition.FullName);
        logger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
        logger.Received().Information("Skipping patch of {TypeName} as it already contain code for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
        logger.ReceivedCalls().Should().HaveCount(4);
    }

    [Fact]
    public void CreateOverrideIfNeeded_GenericBase() {
        // Arrange
        const string source = """
                              using Railroader_ModInterfaces.Tests.Patchers;
                              namespace Foo.Bar { 
                                  public class TargetType : BaseType<TargetType>, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinitionX(source);
        var typeDefinition = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = new MethodPatcher<IMarker, Patcher>(logger, typeof(BaseType<>), "TargetMethod", "InjectedMethod");

        // Act
        var first = sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Name!.Name = "patched";
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        first.Should().BeTrue();
        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "TargetMethod", typeDefinition.FullName);
        logger.Received().Debug("Created {MethodName} override with base call in {TypeName}", "TargetMethod", typeDefinition.FullName);
        logger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
        logger.ReceivedCalls().Should().HaveCount(3);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    public void TargetMethodWithArguments(string suffix) {
        // Arrange
        const string source = """
                              using Railroader_ModInterfaces.Tests.Patchers;
                              namespace Foo.Bar { 
                                  public class TargetType : BaseType, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = AssemblyTestUtils.BuildAssemblyDefinitionX(source, suffix);
        var typeDefinition = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger       = Substitute.For<ILogger>();
        var targetMethod = "TargetMethod" + suffix;

        var sut = new MethodPatcher<IMarker, Patcher>(logger, typeof(BaseType), targetMethod, "InjectedMethod");

        // Act
        var first = sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Name!.Name = "patched";
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        first.Should().BeTrue();
        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", targetMethod, typeDefinition.FullName);
        logger.Received().Debug("Created {MethodName} override with base call in {TypeName}", targetMethod, typeDefinition.FullName);
        logger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
        logger.ReceivedCalls().Should().HaveCount(3);
    }
}

[UsedImplicitly]
public interface IMarker;

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class BaseType
{
    public virtual void TargetMethod() {
    }

    public virtual void TargetMethod1(string arg) {
    }

    public virtual void TargetMethod2(string arg1, string arg2) {
    }

    public virtual void TargetMethod3(string arg1, string arg2, string arg3) {
    }

    public virtual void TargetMethod4(string arg1, string arg2, string arg3, string arg4) {
    }

    public virtual void TargetMethod5(string arg1, string arg2, string arg3, string arg4, string arg5) {
    }

    public virtual void TargetMethod6(DateTime arg1, object arg2, int arg3, decimal arg4, (string, int, bool?) arg5) {
    }

    public virtual void TargetMethod7(DateTime arg1, ref object arg2, out int arg3) {
        arg3 = 42;
    }

}

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class Patcher
{
    public static void InjectedMethod() {
    }
}

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class BaseType<T> where T: BaseType<T>
{
    public virtual void TargetMethod() {
    }
}
