using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NSubstitute;
using Railroader.ModManager.Exceptions;
using Railroader.ModManager.Features.CodePatchers;
using Serilog;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Railroader.ModManager.Tests.Features.CodePatchers;

public class TestMethodPatcher
{
    [Theory]
    [InlineData("NotExistingMethod", "Injected method must be public and static.")]
    [InlineData("PrivateInstanceMethod", "Injected method must be public and static.")]
    [InlineData("PublicInstanceMethod", "Injected method must be public and static.")]
    [InlineData("PrivateStaticMethod", "Injected method must be public and static.")]
    [InlineData("PublicStaticMethodWithInvalidParameters", "Injected method must have single parameter assignable from Railroader.ModManager.Tests.Features.CodePatchers.IMarker.")]
    [InlineData("PublicStaticMethodWithBoolReturnType", "Injected method must bave void return type.")]
    public void ThrowInvalidInjectorMethod(string injectorMethod, string error) {
        // Arrange
        var logger = Substitute.For<ILogger>();

        // Act
        var act = () => MethodPatcher.Factory<IMarker>(logger, typeof(Patcher), typeof(BaseType), "TargetMethod1", injectorMethod);

        // Assert
        act.Should().Throw<ValidationException>()
           .WithMessage("Failed to resolve injected method. See errors for details.")
           .Which.Errors.Should().BeEquivalentTo(error);
    }

    [Fact]
    public void ThrowInvalidInjectorClass() {
        // Arrange
        var logger = Substitute.For<ILogger>();

        // Act
        var act = () => MethodPatcher.Factory<IMarker>(logger, typeof(InternalPatcher), typeof(BaseType), "TargetMethod1", "PublicStaticMethod");

        // Assert
        act.Should().Throw<ValidationException>()
           .WithMessage("Failed to resolve injected method. See errors for details.")
           .Which.Errors.Should().BeEquivalentTo("Injected method declaring type must be public.");
    }

    [Theory]
    [InlineData("1", """
                     namespace Foo.Bar { 
                        public class TargetType { }
                     }
                     """)]
    [InlineData("2", """
                     using Railroader.ModManager.Tests.Features.CodePatchers;

                     namespace Foo.Bar { 
                        public class TargetType : IMarker { } 
                     }
                     """)]
    [InlineData("3", """
                     using Railroader.ModManager.Tests.Features.CodePatchers;

                     namespace Foo.Bar { 
                        public class TargetType : BaseType { } 
                     }
                     """)]
    public void SkipNotMarkedTypes(string suffix, string source) {
        // Arrange
        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source, suffix);
        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = MethodPatcher.Factory<IMarker>(logger, typeof(Patcher), typeof(BaseType), "TargetMethod1", "PublicStaticMethod");

        // Act
        var actual = sut(assemblyDefinition, typeDefinition);
        TestUtils.Write(assemblyDefinition, outputPath, "patched");

        // Assert
        actual.Should().BeFalse();

        logger.Received().Debug("Skipping patching for type {TypeName}: not derived from {BaseType} or does not implement {MarkerInterface}", typeDefinition.FullName, typeof(BaseType), typeof(IMarker));
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void ErrorIfTargetMethodNotExists() {
        // Arrange
        const string source = """
                              using Railroader.ModManager.Tests.Features.CodePatchers;

                              namespace Foo.Bar { 
                                  public class TargetType : BaseType, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);
        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = MethodPatcher.Factory<IMarker>(logger, typeof(Patcher), typeof(BaseType), "NotExistingMethod", "PublicStaticMethod");

        // Act
        var actual = sut(assemblyDefinition, typeDefinition);
        TestUtils.Write(assemblyDefinition, outputPath, "patched");

        // Assert
        actual.Should().BeFalse();

        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "NotExistingMethod", typeDefinition.FullName);
        logger.Received().Error("Virtual method '{MethodName}' not found in {TypeName} hierarchy!", "NotExistingMethod", typeDefinition.FullName);
        logger.ReceivedCalls().Should().HaveCount(2);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    public void CreateOverrideIfNeeded(string suffix) {
        // Arrange
        const string source = """
                              using System;
                              using Railroader.ModManager.Tests.Features.CodePatchers;

                              namespace Foo.Bar { 
                                  public class TargetType : BaseType, IMarker {
                                  }
                              }
                              """;

        var targetMethod = "TargetMethod" + suffix;

        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source, suffix);
        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = MethodPatcher.Factory<IMarker>(logger, typeof(Patcher), typeof(BaseType), targetMethod, "PublicStaticMethod");

        // Act
        var actual = sut(assemblyDefinition, typeDefinition);
        TestUtils.Write(assemblyDefinition, outputPath, "patched");

        // Assert
        actual.Should().BeTrue();

        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", targetMethod, typeDefinition.FullName);
        logger.Received().Debug("Created {MethodName} override with base call in {TypeName}", targetMethod, typeDefinition.FullName);
        logger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker));
        logger.ReceivedCalls().Should().HaveCount(3);

        var baseMethodDef = typeDefinition.BaseType!.Resolve()!.Methods.First(m => m.Name == targetMethod && m.IsVirtual);

        var methodAttributes = baseMethodDef.Attributes & ~(MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot) |
                               MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

        var targetMethodDefinition = typeDefinition.Methods.Should().Contain(o => o.Name == targetMethod).Which;
        targetMethodDefinition.Attributes.Should().Be(methodAttributes);

        var instructions = targetMethodDefinition.Body.Instructions;

        instructions.Last().OpCode.Should().Be(OpCodes.Ret);

        var injectedMethodRef = (MethodReference)instructions[1]!.Operand!;


        instructions[0]!.OpCode.Should().Be(OpCodes.Ldarg_0);
        instructions[1]!.OpCode.Should().Be(OpCodes.Call);
        injectedMethodRef.FullName.Should().Be("System.Void Railroader.ModManager.Tests.Features.CodePatchers.Patcher::PublicStaticMethod(Railroader.ModManager.Tests.Features.CodePatchers.IMarker)");
        injectedMethodRef.Parameters.Should().HaveCount(1);

        var baseCalls = instructions
                        .Skip(2)
                        .Where(i => i.OpCode == OpCodes.Call &&
                                    (i.Operand as MethodReference)?.FullName!.Contains($"BaseType::{targetMethod}") == true)
                        .ToArray();

        baseCalls.Should().HaveCount(1);

        var baseCallIndex = Array.IndexOf(instructions.ToArray()!, baseCalls[0]!);

        var targetMethodInfo       = typeof(BaseType).GetMethod(targetMethod, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var targetMethodParameters = targetMethodInfo.GetParameters();

        var expectedArgCount = targetMethodParameters.Length + 1; // this + params
        var argLoadInstructions = instructions
                                  .Skip(baseCallIndex - expectedArgCount) // Start EXACTLY expectedArgCount before base call
                                  .Take(expectedArgCount)                 // Take EXACTLY that many
                                  .ToArray();

        argLoadInstructions.Should().HaveCount(expectedArgCount);

        for (var i = 0; i < expectedArgCount; i++) {
            var expectedOpcode = i switch {
                0 => OpCodes.Ldarg_0, // this
                1 => OpCodes.Ldarg_1, // param 0
                2 => OpCodes.Ldarg_2, // param 1
                3 => OpCodes.Ldarg_3, // param 2
                _ => OpCodes.Ldarg_S  // param 3+
            };
            argLoadInstructions[i]!.OpCode.Should().Be(expectedOpcode);

            // Verify operands match parameter names
            if (i >= 3) {
                var paramIndex = i - 1; // 0-based param index
                var paramName  = targetMethodParameters[paramIndex].Name;
                (argLoadInstructions[i]!.Operand as ParameterReference)?.Name.Should().Be(paramName);
            }
        }
    }

    [Fact]
    public void SkipDuplicatePatch() {
        // Arrange
        const string source = """
                              using Railroader.ModManager.Tests.Features.CodePatchers;

                              namespace Foo.Bar { 
                                  public class TargetType : BaseType, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);
        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = MethodPatcher.Factory<IMarker>(logger, typeof(Patcher), typeof(BaseType), "TargetMethod1", "PublicStaticMethod");

        // Act
        var first = sut(assemblyDefinition, typeDefinition);
        TestUtils.Write(assemblyDefinition, outputPath, "first");

        var second = sut(assemblyDefinition, typeDefinition);
        TestUtils.Write(assemblyDefinition, outputPath, "second");

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "TargetMethod1", typeDefinition.FullName);
        logger.Received().Debug("Created {MethodName} override with base call in {TypeName}", "TargetMethod1", typeDefinition.FullName);
        logger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker));
        logger.Received().Information("Skipping patch of {TypeName} as it already contain code for {PluginInterface}", typeDefinition.FullName, typeof(IMarker));
        logger.ReceivedCalls().Should().HaveCount(4);
    }

    [Fact]
    public void CreateOverrideIfNeeded_GenericBase() {
        // Arrange
        const string source = """
                              using Railroader.ModManager.Tests.Features.CodePatchers;

                              namespace Foo.Bar { 
                                  public class TargetType : BaseType<TargetType>, IMarker { }
                              }
                              """;

        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);
        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

        var logger = Substitute.For<ILogger>();

        var sut = MethodPatcher.Factory<IMarker>(logger, typeof(Patcher), typeof(BaseType<>), "TargetMethod1", "PublicStaticMethod");

        // Act
        var first = sut(assemblyDefinition, typeDefinition);
        TestUtils.Write(assemblyDefinition, outputPath, "patched");

        // Assert
        first.Should().BeTrue();

        logger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "TargetMethod1", typeDefinition.FullName);
        logger.Received().Debug("Created {MethodName} override with base call in {TypeName}", "TargetMethod1", typeDefinition.FullName);
        logger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker));
        logger.ReceivedCalls().Should().HaveCount(3);
    }
}

[UsedImplicitly]
public interface IMarker;

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class BaseType
{
    public virtual void TargetMethod1() {
    }

    public virtual void TargetMethod2(DateTime arg1, object arg2, int arg3, decimal arg4, bool arg5) {
    }

    public virtual DateTime TargetMethod3(double arg1, DateTime arg2) => arg2.AddDays(arg1);

    protected virtual int TargetMethod4(DateTime arg1, out object arg2, ref int arg3) {
        arg2 = arg1;
        return arg3;
    }
}

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed class InternalPatcher
{
    public static void PublicStaticMethod(IMarker instance) {
    }
}

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class Patcher
{
    private void PrivateInstanceMethod() {
    }

    public void PublicInstanceMethod() {
    }

    private static void PrivateStaticMethod() {
    }

    public static void PublicStaticMethodWithInvalidParameters() {
    }

    public static bool PublicStaticMethodWithBoolReturnType(IMarker instance) => true;

    public static void PublicStaticMethod(IMarker instance) {
    }
}

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class BaseType<T> where T : BaseType<T>
{
    public virtual void TargetMethod1() {
    }
}
