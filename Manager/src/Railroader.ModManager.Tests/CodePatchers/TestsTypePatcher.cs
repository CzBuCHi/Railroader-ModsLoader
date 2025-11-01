using System;
using FluentAssertions;
using Mono.Cecil;
using NSubstitute;
using Railroader.ModManager.CodePatchers;

namespace Railroader.ModManager.Tests.CodePatchers;

public sealed class TestsTypePatcher
{
    [Fact]
    public void CallAllMethodPatchers() {
        // Arrange
        var methodPatcher1 = Substitute.For<IMethodPatcher>();
        var methodPatcher2 = Substitute.For<IMethodPatcher>();
        methodPatcher2.Patch(Arg.Any<AssemblyDefinition>(), Arg.Any<TypeDefinition>()).Returns(true);
        var sut = new TestTypePatcher([methodPatcher1, methodPatcher2]);

        
        var assemblyDefinition = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Name", new Version(1, 0)), "ModuleName", ModuleKind.Dll);
        var typeDefinition     = new TypeDefinition("Namespace", "Name", TypeAttributes.Class);

        // Act
        var actual = sut.Patch(assemblyDefinition, typeDefinition);

        // Assert
        actual.Should().BeTrue();

        methodPatcher1.Received(1).Patch(assemblyDefinition, typeDefinition);
        methodPatcher1.ReceivedCalls().Should().HaveCount(1);

        methodPatcher2.Received(1).Patch(assemblyDefinition, typeDefinition);
        methodPatcher2.ReceivedCalls().Should().HaveCount(1);
    }
}

public sealed class TestTypePatcher(IMethodPatcher[] methodPatchers) : TypePatcher(methodPatchers);
