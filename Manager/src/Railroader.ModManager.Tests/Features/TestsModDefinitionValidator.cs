using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Features;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.JsonConverters;
using Serilog;
using Shouldly;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsModDefinitionValidator {
    private static ModDefinition CreateModDefinition(string id, string version, Dictionary<string, FluentVersion?>? requires = null, Dictionary<string, FluentVersion?>? conflicts = null) =>
        new() {
            Identifier    = id,
            Name          = $"{id} Mod",
            Version       = VersionJsonConverter.ParseString(version)!,
            Requires      = requires,
            ConflictsWith = conflicts
        };

    [Fact]
    public void Valid() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", null }, { "C", null } }),
            CreateModDefinition("B", "1.0.0", new() { { "C", null } }),
            CreateModDefinition("C", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).ToArray().ShouldBeEquivalentTo(new[] { "C", "B", "A" });
        logger.ShouldReceiveNoCalls();
    }

    [Fact]
    public void MissingRequirement() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", null } }),
            CreateModDefinition("C", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Mod '{identifier}' requires mod '{requiredId}', but it is not present.", "A", "B");
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
            CreateModDefinition("A", "1.0.0", new() { { "B", fluentVersion } }),
            CreateModDefinition("B", version)
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        if (isValid) {
            result.Select(mod => mod.Identifier).ToArray().ShouldBeEquivalentTo(new[] { "B", "A" });
        } else {
            result.ShouldBeEmpty();
            logger.Received().Error("Mod '{identifier}' requires mod '{requiredId}' with version constraint '{fluentVersion}', but found version '{version}'.", "A", "B", Arg.Any<FluentVersion>(), Arg.Any<Version>());
        }
    }

    [Fact]
    public void ConflictDetected() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new() { { "B", new(new(1, 0, 0), VersionOperator.GreaterOrEqual) } }),
            CreateModDefinition("B", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Mod '{identifier}' conflicts with mod '{conflictId}' (version: '{version}', constraint: '{fluentVersion}').", "A", "B", Arg.Any<Version>(), Arg.Any<FluentVersion>());
    }

    [Fact]
    public void ConflictWithoutVersion() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new() { { "B", null } }),
            CreateModDefinition("B", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Mod '{identifier}' conflicts with mod '{conflictId}' (version: '{version}').", "A", "B", Arg.Any<Version>());
    }

    [Fact]
    public void ConflictWithNotInstalledMod() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new() { { "B", null } })
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).ToArray().ShouldBeEquivalentTo(new[] { "A" });
        logger.ShouldReceiveNoCalls();
    }

    [Fact]
    public void ConflictWithInstalledModWithValidVersion() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", conflicts: new() { { "B", new(new(1, 0, 0), VersionOperator.LessThan) } }),
            CreateModDefinition("B", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).ToArray().ShouldBeEquivalentTo(new[] { "A", "B" });
        logger.ShouldReceiveNoCalls();
    }

    [Fact]
    public void CyclicDependency() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", null } }),
            CreateModDefinition("B", "1.0.0", new() { { "C", null } }),
            CreateModDefinition("C", "1.0.0", new() { { "A", null } }),
            CreateModDefinition("D", "1.0.0", new() { { "E", null } }),
            CreateModDefinition("E", "1.0.0", new() { { "D", null } })
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Cyclic dependency detected: {dependencyLoop}", "A -> B -> C -> A");
        logger.Received().Error("Cyclic dependency detected: {dependencyLoop}", "D -> E -> D");
    }

    [Fact]
    public void MissingDependencyInSort() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", null } }),
            CreateModDefinition("C", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Mod '{identifier}' requires mod '{requiredId}', but it is not present.", "A", "B");
    }

    [Fact]
    public void MultipleErrors() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", null }, { "C", new(new(2, 0, 0), VersionOperator.GreaterOrEqual) } }),
            CreateModDefinition("C", "1.0.0", conflicts: new() { { "A", new(new(1, 0, 0)) } }),
            CreateModDefinition("D", "1.0.0", new() { { "E", null } })
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Mod '{identifier}' requires mod '{requiredId}', but it is not present.", "A", "B");
        logger.Received().Error("Mod '{identifier}' requires mod '{requiredId}' with version constraint '{fluentVersion}', but found version '{version}'.", "A", "C", Arg.Any<FluentVersion>(), Arg.Any<Version>());
        logger.Received().Error("Mod '{identifier}' conflicts with mod '{conflictId}' (version: '{version}', constraint: '{fluentVersion}').", "C", "A", Arg.Any<Version>(), Arg.Any<FluentVersion>());
        logger.Received().Error("Mod '{identifier}' requires mod '{requiredId}', but it is not present.", "D", "E");
    }

    [Fact]
    public void InvalidVersionOperator() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", new(new(1, 0, 0), (VersionOperator)999) } }),
            CreateModDefinition("B", "1.0.0")
        };

        // Act
        Should.Throw<InvalidOperationException>(() => ModDefinitionValidator.ValidateAndSort(logger, modDefinitions))
              .Message.ShouldStartWith("Unknown version operator:");
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
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).ToArray().ShouldBeEquivalentTo(new[] { "A", "B", "C" });
    }

    [Fact]
    public void CycleCausingMissingDependency() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", null } }),
            CreateModDefinition("B", "1.0.0", new() { { "C", null } }),
            CreateModDefinition("C", "1.0.0", new() { { "A", null } }),
            CreateModDefinition("D", "1.0.0", new() { { "C", null } })
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Cyclic dependency detected: {dependencyLoop}", "A -> B -> C -> A");
        logger.Received().Error("Mod '{identifier}' cannot resolve mod '{requiredId}' because mod is part of a cyclic dependency.", "D", "C");
    }

    [Fact]
    public void NonCyclicRevisitedMod() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var modDefinitions = new[] {
            CreateModDefinition("A", "1.0.0", new() { { "B", null }, { "C", null } }),
            CreateModDefinition("B", "1.0.0", new() { { "D", null } }),
            CreateModDefinition("C", "1.0.0", new() { { "D", null } }),
            CreateModDefinition("D", "1.0.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, modDefinitions);

        // Assert
        result.Select(mod => mod.Identifier).ToArray().ShouldBeEquivalentTo(new[] { "D", "B", "C", "A" });
    }

    [Fact]
    public void TwoConflicts_OriginalAccumulates_MutantWouldReset()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var mods = new[] {
            CreateModDefinition("A", "1.0", conflicts: new() { { "B", null }, { "C", null } }),
            CreateModDefinition("B", "1.0"),
            CreateModDefinition("C", "1.0")
        };

        // Act
        var result = ModDefinitionValidator.ValidateAndSort(logger, mods);

        // Assert
        result.ShouldBeEmpty();
        logger.Received().Error("Mod '{identifier}' conflicts with mod '{conflictId}' (version: '{version}').", "A", "B", Arg.Any<Version>());
        logger.Received().Error("Mod '{identifier}' conflicts with mod '{conflictId}' (version: '{version}').", "A", "C", Arg.Any<Version>());
    }

    [Fact]
    public void TwoSeparateCycles_OriginalDetectsBoth_MutantMissesSecond()
    {
        var logger = Substitute.For<ILogger>();
        var mods = new[] {
            CreateModDefinition("A", "1.0", new() { { "B", null } }),
            CreateModDefinition("B", "1.0", new() { { "A", null } }),
            CreateModDefinition("C", "1.0", new() { { "D", null } }),
            CreateModDefinition("D", "1.0", new() { { "C", null } })
        };

        var result = ModDefinitionValidator.ValidateAndSort(logger, mods);
        result.ShouldBeEmpty();
        logger.Received(2).Error("Cyclic dependency detected: {dependencyLoop}", Arg.Any<string>());
    }
}