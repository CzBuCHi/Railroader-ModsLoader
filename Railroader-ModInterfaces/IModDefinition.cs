using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog.Events;

namespace Railroader.ModInterfaces;

/// <summary> Defines the metadata for a mod. Loaded from Definition.json </summary>
[PublicAPI]
public interface IModDefinition
{
    /// <summary> Gets the unique identifier for the mod. </summary>
    string Identifier { get; }

    /// <summary> Gets the display name of the mod. </summary>
    string Name { get; }

    /// <summary> Gets the version of the mod. </summary>
    /// <remarks> The version specifies the mod's release in a format compatible with <see cref="System.Version"/>, typically in the form Major.Minor.Build (e.g., "1.0.0"). </remarks>
    Version Version { get; }

    /// <summary> Gets the preferred logging level for the mod, or <see langword="null"/> to use <see cref="LogEventLevel.Information"/>. </summary>
    LogEventLevel? LogLevel { get; }

    /// <summary>Gets the dictionary of mod identifiers and their required version constraints that this mod depends on.</summary>
    /// <remarks>
    /// Each key is a mod identifier, and each value is a <see cref="FluentVersion"/> specifying the version constraint, or <c>null</c> to indicate any version is acceptable.
    /// In JSON deserialization:
    /// <list type="bullet">
    ///   <item>A version string without an operator (e.g., <c>"1.0.1"</c>) sets <see cref="FluentVersion.Operator"/> to <see cref="VersionOperator.GreaterOrEqual"/> and <see cref="FluentVersion.Version"/> to the parsed version.</item>
    ///   <item>An empty string (e.g., <c>""</c>) results in a <c>null</c> value in the dictionary, indicating any version of the mod is acceptable.</item>
    ///   <item>A string with an operator (e.g., <c>"=1.0.1"</c>, <c>"&gt;=1.0.1"</c>, <c>"&lt;2.0.0"</c>) sets the corresponding <see cref="FluentVersion.Operator"/> and <see cref="FluentVersion.Version"/>.</item>
    /// </list>
    /// For example, <c>"foo": "1.0.1"</c> requires mod "foo" with version 1.0.1 or higher, <c>"foo": "=1.0.1"</c> requires exactly version 1.0.1, and <c>"foo": ""</c> requires mod "foo" with any version. Version strings must be valid <see cref="System.Version"/> formats, except for the empty string case.
    /// </remarks>
    Dictionary<string, FluentVersion?> Requires { get; }

    /// <summary>Gets the dictionary of mod identifiers and their version constraints that this mod conflicts with.</summary>
    /// <remarks>
    /// Each key is a mod identifier, and each value is a <see cref="FluentVersion"/> specifying the incompatible version constraint, or <c>null</c> to indicate any version is incompatible.
    /// In JSON deserialization:
    /// <list type="bullet">
    ///   <item>A version string without an operator (e.g., <c>"1.2.3"</c>) sets <see cref="FluentVersion.Operator"/> to <see cref="VersionOperator.GreaterOrEqual"/>, meaning versions at or above the specified version are incompatible.</item>
    ///   <item>An empty string (e.g., <c>""</c>) results in a <c>null</c> value in the dictionary, indicating any version of the mod is incompatible.</item>
    ///   <item>A string with an operator (e.g., <c>"=1.0.1"</c>, <c>"&lt;1.2.3"</c>, <c>"&gt;1.0.1"</c>) sets the corresponding <see cref="FluentVersion.Operator"/> and <see cref="FluentVersion.Version"/>.</item>
    /// </list>
    /// For example, <c>"second": "&lt;1.2.3"</c> means versions of mod "second" less than 1.2.3 are incompatible, <c>"second": "=1.0.1"</c> means only version 1.0.1 is incompatible, and <c>"second": ""</c> means any version of mod "second" is incompatible.
    /// Version strings must be valid <see cref="System.Version"/> formats, except for the empty string case.
    /// </remarks>
    Dictionary<string, FluentVersion?> ConflictsWith { get; }
}
