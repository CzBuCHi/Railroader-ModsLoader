using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Serilog.Events;

namespace Railroader.ModInterfaces;

/// <summary> Defines some basic information about a mod. This is a subset of the data contained within the Definition.json. </summary>
[PublicAPI]
public interface IModDefinition
{
    /// <summary> Defines an unique ID for the mod, used for e.g. referencing other mods. </summary>
    string Id { get; }

    /// <summary> Name of the mod. </summary>
    string Name { get; }

    /// <summary> Version of the mod. </summary>
    Version Version { get; }

    /// <summary> Log level used by mod. <see cref="LogEventLevel.Information"/> is used if missing</summary>
    LogEventLevel? LogLevel { get; }

    /// <summary> Path to directory where Definition.json is. </summary>
    public string DefinitionPath { get; set; }
}

