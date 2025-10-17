using System;
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
    Version Version { get; }

    /// <summary> Gets the preferred logging level for the mod, or <see langword="null"/> to use <see cref="LogEventLevel.Information"/>. </summary>
    LogEventLevel? LogLevel { get; }
}