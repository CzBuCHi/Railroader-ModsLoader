using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Railroader.ModManager.Features;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.JsonConverters;
using Serilog;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsModDefinitionValidator
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
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null }, { "C", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } }),
            CreateModDefinition("C", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).Should().Equal("C", "B", "A");
        logger.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void MissingRequirement() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("C", "1.0.0")
        };

        string[] expected = ["Mod 'A' requires mod 'B', but it is not present."];

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Should().BeEmpty();
        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
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
        var logger        = Substitute.For<ILogger>();
        var fluentVersion = new FluentVersion(Version.Parse(requiredVersion), @operator);
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", fluentVersion } }),
            CreateModDefinition("B", version)
        };

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        if (isValid) {
            result.Select(mod => mod.Identifier).Should().Equal("B", "A");
        } else {
            string[] expected = [$"Mod 'A' requires mod 'B' with version constraint '{fluentVersion}', but found version '{version}'."];
            result.Should().BeEmpty();
            logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
        }
    }

    [Fact]
    public void ConflictDetected() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new Dictionary<string, FluentVersion?> { { "B", new FluentVersion(new Version(1, 0, 0), VersionOperator.GreaterOrEqual) } }),
            CreateModDefinition("B", "1.0.0")
        };
        string[] expected = ["Mod 'A' conflicts with mod 'B' (version: '1.0.0', constraint: '>=1.0.0')."];

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Should().BeEmpty();
        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
    }

    [Fact]
    public void ConflictWithoutVersion() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("B", "1.0.0")
        };
        string[] expected = ["Mod 'A' conflicts with mod 'B' (version: '1.0.0')."];

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Should().BeEmpty();
        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
    }

    [Fact]
    public void CyclicDependency() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } }),
            CreateModDefinition("C", "1.0.0", new Dictionary<string, FluentVersion?> { { "A", null } }),
            CreateModDefinition("D", "1.0.0", new Dictionary<string, FluentVersion?> { { "E", null } }),
            CreateModDefinition("E", "1.0.0", new Dictionary<string, FluentVersion?> { { "D", null } })
        };
        string[] expected = [
            "Cyclic dependency detected: A -> B -> C -> A",
            "Cyclic dependency detected: D -> E -> D"
        ];

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Should().BeEmpty();
        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
    }

    [Fact]
    public void MissingDependencyInSort() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("C", "1.0.0")
        };
        string[] expected = ["Mod 'A' requires mod 'B', but it is not present."];

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Should().BeEmpty();
        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
    }

    [Fact]
    public void MultipleErrors() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null }, { "C", new FluentVersion(new Version(2, 0, 0), VersionOperator.GreaterOrEqual) } }),
            CreateModDefinition("C", "1.0.0", conflicts: new Dictionary<string, FluentVersion?> { { "A", new FluentVersion(new Version(1, 0, 0)) } }),
            CreateModDefinition("D", "1.0.0", new Dictionary<string, FluentVersion?> { { "E", null } })
        };
        string[] expected = [
            "Mod 'A' requires mod 'B', but it is not present.",
            "Mod 'A' requires mod 'C' with version constraint '>=2.0.0', but found version '1.0.0'.",
            "Mod 'C' conflicts with mod 'A' (version: '1.0.0', constraint: '1.0.0').",
            "Mod 'D' requires mod 'E', but it is not present."
        ];

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Should().BeEmpty();
        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
    }

    [Fact]
    public void InvalidVersionOperator() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", new FluentVersion(new Version(1, 0, 0), (VersionOperator)999) } }),
            CreateModDefinition("B", "1.0.0")
        };
        
        // Act
        var act = () => ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Unknown version operator: *");
    }

    [Fact]
    public void NoDependencies() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0"),
            CreateModDefinition("B", "1.0.0"),
            CreateModDefinition("C", "1.0.0")
        };
        
        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).Should().Contain(["A", "B", "C"]);
    }

    [Fact]
    public void CycleCausingMissingDependency() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } }),
            CreateModDefinition("C", "1.0.0", new Dictionary<string, FluentVersion?> { { "A", null } }),
            CreateModDefinition("D", "1.0.0", new Dictionary<string, FluentVersion?> { { "C", null } })
        };
        string[] expected = [
            "Cyclic dependency detected: A -> B -> C -> A",
            "Mod 'D' cannot resolve mod 'C' because mod 'C' is part of a cyclic dependency."
        ];

        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Should().BeEmpty();
        logger.Received().Error("Mod preprocessing failed with error(s): {errors}", Arg.Is<string[]>(o => o.SequenceEqual(expected)));
    }

    [Fact]
    public void NonCyclicRevisitedMod() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new Dictionary<string, FluentVersion?> { { "B", null }, { "C", null } }),
            CreateModDefinition("B", "1.0.0", new Dictionary<string, FluentVersion?> { { "D", null } }),
            CreateModDefinition("C", "1.0.0", new Dictionary<string, FluentVersion?> { { "D", null } }),
            CreateModDefinition("D", "1.0.0")
        };
        
        // Act
        var result = ModDefinitionValidator.Execute(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).Should().Equal("D", "B", "C", "A");
    }
}
