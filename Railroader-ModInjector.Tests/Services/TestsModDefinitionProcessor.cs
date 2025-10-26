using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector.JsonConverters;
using Railroader.ModInjector.Services;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.Tests.Services;

public sealed class TestsModDefinitionProcessor
{
    private static ModDefinition CreateModDefinition(string id, string version, Dictionary<string, FluentVersion?>? requires = null, Dictionary<string, FluentVersion?>? conflicts = null) =>
        new() {
            Identifier = id,
            Name = $"{id} Mod",
            Version = VersionJsonConverter.ParseString(version)!,
            Requires = requires,
            ConflictsWith = conflicts
        };

    [Fact]
    public void Valid() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null }, { "C", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } }),
            CreateModDefinition("C", "1.0.0")
        };
        var sut = new ModDefinitionProcessor();

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeTrue();
        sut.Errors.Should().BeEmpty();
        modDefinitions.Select(mod => mod.Identifier).Should().Equal("C", "B", "A");
    }
    
    [Fact]
    public void MissingRequirement() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("C", "1.0.0")
        };
        var      sut      = new ModDefinitionProcessor();
        string[] expected = ["Mod 'A' requires mod 'B', but it is not present."];

        var logger = Substitute.For<ILogger>();
        DI.GetLogger = _ => logger;
        
        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeFalse();
        sut.Errors.Should().BeEquivalentTo(expected);
        modDefinitions.Should().BeEmpty();

        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(sut.Errors)));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0", VersionOperator.Equal, true)]
    [InlineData("1.0.0", "1.0.1", VersionOperator.Equal, false)]
    [InlineData("1.0.0", "1.0.0", VersionOperator.GreaterThan, false)]
    [InlineData("1.0.0", "1.0.1", VersionOperator.GreaterThan, false)]
    [InlineData("1.0.1", "1.0.0", VersionOperator.GreaterThan, true)]
    [InlineData("1.0.0", "1.0.0", VersionOperator.GreaterOrEqual, true)]
    [InlineData("1.0.0", "1.0.1", VersionOperator.GreaterOrEqual, false)]
    [InlineData("1.0.1", "1.0.0", VersionOperator.GreaterOrEqual, true)]
    [InlineData("1.0.0", "1.0.0", VersionOperator.LessOrEqual, true)]
    [InlineData("1.0.1", "1.0.0", VersionOperator.LessOrEqual, false)]
    [InlineData("1.0.0", "1.0.1", VersionOperator.LessOrEqual, true)]
    [InlineData("1.0.0", "1.0.0", VersionOperator.LessThan, false)]
    [InlineData("1.0.1", "1.0.0", VersionOperator.LessThan, false)]
    [InlineData("1.0.0", "1.0.1", VersionOperator.LessThan, true)]
    public void RequiredVersion(string version, string requiredVersion, VersionOperator @operator, bool isValid) {
        // Arrange
        var fluentVersion = new FluentVersion(Version.Parse(requiredVersion), @operator);
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", fluentVersion } }),
            CreateModDefinition("B", version)
        };
        var sut = new ModDefinitionProcessor();
        
        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        if (isValid) {
            result.Should().BeTrue();
            sut.Errors.Should().BeEmpty();
            modDefinitions.Select(mod => mod.Identifier).Should().Equal("B", "A");
        } else {
            string[] expected = [$"Mod 'A' requires mod 'B' with version constraint '{fluentVersion}', but found version '{version}'."];
            result.Should().BeFalse();
            sut.Errors.Should().BeEquivalentTo(expected);
            modDefinitions.Should().BeEmpty();
        }
    }
  
    [Fact]
    public void ConflictDetected() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new Dictionary<string, FluentVersion?> { { "B", new FluentVersion(new Version(1, 0, 0), VersionOperator.GreaterOrEqual) } }),
            CreateModDefinition("B", "1.0.0")
        };
        var      sut      = new ModDefinitionProcessor();
        string[] expected = ["Mod 'A' conflicts with mod 'B' (version: '1.0.0', constraint: '>=1.0.0')."];

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeFalse();
        sut.Errors.Should().BeEquivalentTo(expected);
        modDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void ConflictWithoutVersion() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("B", "1.0.0")
        };
        var      sut      = new ModDefinitionProcessor();
        string[] expected = ["Mod 'A' conflicts with mod 'B' (version: '1.0.0')."];

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeFalse();
        sut.Errors.Should().BeEquivalentTo(expected);
        modDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void CyclicDependency() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } }),
            CreateModDefinition("C", "1.0.0", new Dictionary<string, FluentVersion?> { { "A", null } }),
            CreateModDefinition("D", "1.0.0", new Dictionary<string, FluentVersion?> { { "E", null } }),
            CreateModDefinition("E", "1.0.0", new Dictionary<string, FluentVersion?> { { "D", null } }),
        };
        var      sut      = new ModDefinitionProcessor();
        string[] expected = [
            "Cyclic dependency detected: A -> B -> C -> A",
            "Cyclic dependency detected: D -> E -> D",
        ];

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeFalse();
        sut.Errors.Should().BeEquivalentTo(expected);
        modDefinitions.Should().BeEmpty();
    }
    
    [Fact]
    public void MissingDependencyInSort() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("C", "1.0.0")
        };
        var      sut      = new ModDefinitionProcessor();
        string[] expected = ["Mod 'A' requires mod 'B', but it is not present."];

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeFalse();
        sut.Errors.Should().BeEquivalentTo(expected);
        modDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void MultipleErrors() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null }, { "C", new FluentVersion(new Version(2, 0, 0), VersionOperator.GreaterOrEqual) } }),
            CreateModDefinition("C", "1.0.0", conflicts: new Dictionary<string, FluentVersion?> { { "A", new FluentVersion(new Version(1, 0, 0)) } }),
            CreateModDefinition("D", "1.0.0", new Dictionary<string, FluentVersion?> { { "E", null } })
        };
        var sut = new ModDefinitionProcessor();
        string[] expected = [
            "Mod 'A' requires mod 'B', but it is not present.",
            "Mod 'A' requires mod 'C' with version constraint '>=2.0.0', but found version '1.0.0'.",
            "Mod 'C' conflicts with mod 'A' (version: '1.0.0', constraint: '1.0.0').",
            "Mod 'D' requires mod 'E', but it is not present."
        ];

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeFalse();
        sut.Errors.Should().BeEquivalentTo(expected);
        modDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void InvalidVersionOperator() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", new FluentVersion(new Version(1, 0, 0), (VersionOperator)999) } }),
            CreateModDefinition("B", "1.0.0")
        };
        var sut = new ModDefinitionProcessor();

        // Act
        Action act = () => sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Unknown version operator: *");
        
        sut.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NoDependencies() {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0"),
            CreateModDefinition("B", "1.0.0"),
            CreateModDefinition("C", "1.0.0")
        };
        var sut = new ModDefinitionProcessor();

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeTrue();
        sut.Errors.Should().BeEmpty();
        modDefinitions.Should().HaveCount(3);
        modDefinitions.Select(mod => mod.Identifier).Should().Contain("A", "B", "C");
    }

    [Fact]
    public void CycleCausingMissingDependency()
    {
        // Arrange
        var modDefinitions = new[]
        {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } }),
            CreateModDefinition("C", "1.0.0", new Dictionary<string, FluentVersion?> { { "A", null } }),
            CreateModDefinition("D", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } }),
        };
        var sut = new ModDefinitionProcessor();
        string[] expected = [
            "Cyclic dependency detected: A -> B -> C -> A",
            "Mod 'D' cannot resolve mod 'C' because mod 'C' is part of a cyclic dependency."
        ];

        // Act
        bool result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeFalse();
        sut.Errors.Should().BeEquivalentTo(expected);
        modDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void NonCyclicRevisitedMod()
    {
        // Arrange
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null }, { "C", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "D", null } }),
            CreateModDefinition("C", "1.0.0", new Dictionary<string, FluentVersion?> { { "D", null } }),
            CreateModDefinition("D", "1.0.0")
        };
        var sut = new ModDefinitionProcessor();

        // Act
        var result = sut.PreprocessModDefinitions(ref modDefinitions);

        // Assert
        result.Should().BeTrue();
        sut.Errors.Should().BeEmpty();
        modDefinitions.Select(mod => mod.Identifier).Should().Equal("D", "B", "C", "A");
    }
}
