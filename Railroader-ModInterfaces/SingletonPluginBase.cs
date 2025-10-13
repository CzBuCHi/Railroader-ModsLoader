using System;
using JetBrains.Annotations;

namespace Railroader.ModInterfaces;

/// <summary> The base class for singleton-based plugins. </summary>
[PublicAPI]
public abstract class SingletonPluginBase<T> : PluginBase where T : SingletonPluginBase<T>
{
    /// <summary> Gets the instance of this plugin, if any has been created. </summary>
    public static T? Instance { get; private set; }

    /// <summary> Creates a new instance of <see cref="T:Railloader.SingletonPluginBase`1" />. </summary>
    /// <param name="moddingContext">Instance of shared <see cref="IModdingContext"/>.</param>
    /// <param name="modDefinition">Instance of <see cref="IModDefinition"/> describing current mod.</param>
    protected SingletonPluginBase(IModdingContext moddingContext, IModDefinition modDefinition) : base(moddingContext, modDefinition) {
        if (Instance != null) {
            throw new InvalidOperationException($"Cannot create singleton plugin '{GetType()}' twice.");
        }

        Instance = (T)this;
    }
}