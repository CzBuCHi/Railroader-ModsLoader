//using System;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Reflection;
//using FluentAssertions;
//using JetBrains.Annotations;
//using Mono.Cecil;
//using Mono.Cecil.Cil;
//using NSubstitute;
//using Railroader.ModManager.CodePatchers;
//using MethodAttributes = Mono.Cecil.MethodAttributes;

//namespace Railroader.ModManager.Tests.CodePatchers;

//public sealed class TestsMethodPatcher
//{
//    [Theory]
//    [InlineData("NotExistingInjectedMethod")]
//    [InlineData("PrivateInjectedMethod")]
//    [InlineData("PrivateStaticInjectedMethod")]
//    [InlineData("PublicInjectedMethod")]
//    public void ThrowInvalidInjectorMethod(string injectorMethod)
//    {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        // Act
//        var act = () => new MethodPatcher<IMarker, TestsMethodPatcher>(serviceManager.LoggerFactory, typeof(BaseType), "TargetMethod1", injectorMethod);

//        // Assert
//        act.Should().Throw<ArgumentException>().WithMessage("Injected method must be public static void method on public class.*");
//    }

//    [UsedImplicitly]
//    [ExcludeFromCodeCoverage]
//    private void PrivateInjectedMethod() { }

//    [UsedImplicitly]
//    [ExcludeFromCodeCoverage]
//    private static void PrivateStaticInjectedMethod() { }

//    [UsedImplicitly]
//    [ExcludeFromCodeCoverage]
//#pragma warning disable xUnit1013
//    public void PublicInjectedMethod() { }
//#pragma warning restore xUnit1013

//    [Theory]
//    [InlineData("1", """
//                     namespace Foo.Bar { 
//                        public class TargetType { }
//                     }
//                     """)]
//    [InlineData("2", """
//                     using Railroader.ModManager.Tests.CodePatchers;

//                     namespace Foo.Bar { 
//                        public class TargetType : IMarker { } 
//                     }
//                     """)]
//    [InlineData("3", """
//                     using Railroader.ModManager.Tests.CodePatchers;

//                     namespace Foo.Bar { 
//                        public class TargetType : BaseType { } 
//                     }
//                     """)]
//    public void SkipNotMarkedTypes(string suffix, string source)
//    {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source, suffix);
//        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");


//        var sut = new MethodPatcher<IMarker, Patcher>(serviceManager.LoggerFactory, typeof(BaseType), "TargetMethod1", "InjectedMethod");

//        // Act
//        var actual = sut.Patch(assemblyDefinition, typeDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "patched");

//        // Assert
//        actual.Should().BeFalse();

//        serviceManager.ContextLogger.Received().Debug("Skipping patching for type {TypeName}: not derived from {BaseType} or does not implement {MarkerInterface}", typeDefinition.FullName, typeof(BaseType), typeof(IMarker));
//        serviceManager.ContextLogger.ReceivedCalls().Should().HaveCount(1);
//    }

//    [Fact]
//    public void ErrorIfTargetMethodNotExists()
//    {
//        // Arrange
//        const string source = """
//                              using Railroader.ModManager.Tests.CodePatchers;
//                              namespace Foo.Bar { 
//                                  public class TargetType : BaseType, IMarker { }
//                              }
//                              """;

//        var serviceManager = new TestServiceManager();

//        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);
//        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");



//        var sut = new MethodPatcher<IMarker, Patcher>(serviceManager.LoggerFactory, typeof(BaseType), "InvalidTargetMethod", "InjectedMethod");

//        // Act
//        var actual = sut.Patch(assemblyDefinition, typeDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "patched");

//        // Assert
//        actual.Should().BeFalse();
//        serviceManager.ContextLogger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "InvalidTargetMethod", typeDefinition.FullName);
//        serviceManager.ContextLogger.Received().Error("Virtual method '{MethodName}' not found in {TypeName} hierarchy!", "InvalidTargetMethod", typeDefinition.FullName);
//        serviceManager.ContextLogger.ReceivedCalls().Should().HaveCount(2);
//    }

//    [Theory]
//    [InlineData("1")]
//    [InlineData("2")]
//    [InlineData("3")]
//    [InlineData("4")]
//    public void CreateOverrideIfNeeded(string suffix)
//    {
//        // Arrange
//        const string source = """
//                              using System;
//                              using Railroader.ModManager.Tests.CodePatchers;

//                              namespace Foo.Bar { 
//                                  public class TargetType : BaseType, IMarker {
//                                  }
//                              }
//                              """;

//        var serviceManager = new TestServiceManager();

//        var targetMethod = "TargetMethod" + suffix;

//        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source, suffix);
//        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

//        var sut = new MethodPatcher<IMarker, Patcher>(serviceManager.LoggerFactory, typeof(BaseType), targetMethod, "InjectedMethod");

//        // Act
//        var actual = sut.Patch(assemblyDefinition, typeDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "patched");

//        // Assert
//        actual.Should().BeTrue();

//        serviceManager.ContextLogger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", targetMethod, typeDefinition.FullName);
//        serviceManager.ContextLogger.Received().Debug("Created {MethodName} override with base call in {TypeName}", targetMethod, typeDefinition.FullName);
//        serviceManager.ContextLogger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
//        serviceManager.ContextLogger.ReceivedCalls().Should().HaveCount(3);

//        var baseMethodDef = typeDefinition.BaseType!.Resolve()!.Methods.First(m => m.Name == targetMethod && m.IsVirtual);

//        var methodAttributes = (baseMethodDef.Attributes & ~(MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot)) |
//                               MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

//        var targetMethodDefinition = typeDefinition.Methods.Should().Contain(o => o.Name == targetMethod).Which;
//        targetMethodDefinition.Attributes.Should().Be(methodAttributes);

//        var instructions = targetMethodDefinition.Body.Instructions;

//        instructions.Last().OpCode.Should().Be(OpCodes.Ret);

//        var injectedMethodRef = (MethodReference)instructions[1]!.Operand!;


//        instructions[0]!.OpCode.Should().Be(OpCodes.Ldarg_0);
//        instructions[1]!.OpCode.Should().Be(OpCodes.Call);
//        injectedMethodRef.FullName.Should().Contain("Patcher::InjectedMethod");
//        injectedMethodRef.Parameters.Should().HaveCount(1);

//        var baseCalls = instructions
//                        .Skip(2)
//                        .Where(i => i.OpCode == OpCodes.Call &&
//                                    (i.Operand as MethodReference)?.FullName!.Contains($"BaseType::{targetMethod}") == true)
//                        .ToArray();

//        baseCalls.Should().HaveCount(1);

//        var baseCallIndex = Array.IndexOf(instructions.ToArray()!, baseCalls[0]!);

//        var targetMethodInfo = typeof(BaseType).GetMethod(targetMethod, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
//        var targetMethodParameters = targetMethodInfo.GetParameters();

//        var expectedArgCount = targetMethodParameters.Length + 1; // this + params
//        var argLoadInstructions = instructions
//                                  .Skip(baseCallIndex - expectedArgCount) // Start EXACTLY expectedArgCount before base call
//                                  .Take(expectedArgCount)                 // Take EXACTLY that many
//                                  .ToArray();

//        argLoadInstructions.Should().HaveCount(expectedArgCount);

//        for (int i = 0; i < expectedArgCount; i++)
//        {
//            var expectedOpcode = i switch
//            {
//                0 => OpCodes.Ldarg_0, // this
//                1 => OpCodes.Ldarg_1, // param 0
//                2 => OpCodes.Ldarg_2, // param 1
//                3 => OpCodes.Ldarg_3, // param 2
//                _ => OpCodes.Ldarg_S  // param 3+
//            };
//            argLoadInstructions[i]!.OpCode.Should().Be(expectedOpcode);

//            // Verify operands match parameter names
//            if (i >= 3)
//            {
//                var paramIndex = i - 1; // 0-based param index
//                var paramName = targetMethodParameters[paramIndex].Name;
//                (argLoadInstructions[i]!.Operand as ParameterReference)?.Name.Should().Be(paramName);
//            }
//        }
//    }

//    [Fact]
//    public void SkipDuplicatePatch()
//    {
//        // Arrange
//        const string source = """
//                              using Railroader.ModManager.Tests.CodePatchers;
//                              namespace Foo.Bar { 
//                                  public class TargetType : BaseType, IMarker { }
//                              }
//                              """;

//        var serviceManager = new TestServiceManager();

//        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);
//        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");

//        var sut = new MethodPatcher<IMarker, Patcher>(serviceManager.LoggerFactory, typeof(BaseType), "TargetMethod1", "InjectedMethod");

//        // Act
//        var first = sut.Patch(assemblyDefinition, typeDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "first");

//        var second = sut.Patch(assemblyDefinition, typeDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "second");

//        // Assert
//        first.Should().BeTrue();
//        second.Should().BeFalse();
//        serviceManager.ContextLogger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "TargetMethod1", typeDefinition.FullName);
//        serviceManager.ContextLogger.Received().Debug("Created {MethodName} override with base call in {TypeName}", "TargetMethod1", typeDefinition.FullName);
//        serviceManager.ContextLogger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
//        serviceManager.ContextLogger.Received().Information("Skipping patch of {TypeName} as it already contain code for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
//        serviceManager.ContextLogger.ReceivedCalls().Should().HaveCount(4);
//    }

//    [Fact]
//    public void CreateOverrideIfNeeded_GenericBase()
//    {
//        // Arrange
//        const string source = """
//                              using Railroader.ModManager.Tests.CodePatchers;
//                              namespace Foo.Bar { 
//                                  public class TargetType : BaseType<TargetType>, IMarker { }
//                              }
//                              """;

//        var serviceManager = new TestServiceManager();

//        var (assemblyDefinition, outputPath) = TestUtils.BuildAssemblyDefinition(source);
//        var typeDefinition = assemblyDefinition.MainModule.Types.First(o => o.FullName == "Foo.Bar.TargetType");



//        var sut = new MethodPatcher<IMarker, Patcher>(serviceManager.LoggerFactory, typeof(BaseType<>), "TargetMethod1", "InjectedMethod");

//        // Act
//        var first = sut.Patch(assemblyDefinition, typeDefinition);
//        TestUtils.Write(assemblyDefinition, outputPath, "patched");

//        // Assert
//        first.Should().BeTrue();
//        serviceManager.ContextLogger.Received().Debug("{MethodName} method not found in {TypeName}, creating override", "TargetMethod1", typeDefinition.FullName);
//        serviceManager.ContextLogger.Received().Debug("Created {MethodName} override with base call in {TypeName}", "TargetMethod1", typeDefinition.FullName);
//        serviceManager.ContextLogger.Received().Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IMarker).FullName);
//        serviceManager.ContextLogger.ReceivedCalls().Should().HaveCount(3);
//    }
//}


//[UsedImplicitly]
//public interface IMarker;

//[ExcludeFromCodeCoverage]
//[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
//public abstract class BaseType
//{
//    public virtual void TargetMethod1()
//    {
//    }

//    public virtual void TargetMethod2(DateTime arg1, object arg2, int arg3, decimal arg4, bool arg5)
//    {
//    }

//    public virtual DateTime TargetMethod3(double arg1, DateTime arg2)
//    {
//        return arg2.AddDays(arg1);
//    }

//    protected virtual int TargetMethod4(DateTime arg1, out object arg2, ref int arg3)
//    {
//        arg2 = arg1;
//        return arg3;
//    }
//}

//[ExcludeFromCodeCoverage]
//[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
//public sealed class Patcher
//{
//    public static void InjectedMethod(BaseType instance)
//    {
//    }
//}

//[ExcludeFromCodeCoverage]
//[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
//public abstract class BaseType<T> where T : BaseType<T>
//{
//    public virtual void TargetMethod1()
//    {
//    }
//}

