using System;
using JetBrains.Annotations;

namespace Railroader.ModInterfaces;

/// <summary> Base class for singleton plugins, ensuring only one instance exists per type. </summary>
/// <typeparam name="T">The concrete singleton plugin type.</typeparam>
[PublicAPI]
public abstract class SingletonPluginBase<T> : PluginBase where T : SingletonPluginBase<T>
{
    private static T? _Instance;

    /// <summary> Gets the singleton instance of this plugin type. </summary>
    /// <exception cref="InvalidOperationException"> Thrown if the instance has not been created yet. </exception>
    public static T Instance => _Instance ?? throw new InvalidOperationException($"{typeof(T).Name} was not created");

    /// <summary> Initializes a new instance of the <see cref="SingletonPluginBase{T}"/> class. </summary>
    /// <param name="moddingContext">The modding context.</param>
    /// <param name="modDefinition">The mod definition.</param>
    /// <exception cref="InvalidOperationException"> Thrown if an instance of this type already exists. </exception>
    protected SingletonPluginBase(IModdingContext moddingContext, IModDefinition modDefinition) 
        : base(moddingContext, modDefinition)
    {
        if (_Instance != null) {
            throw new InvalidOperationException($"Cannot create singleton plugin '{GetType()}' twice.");
        }

        _Instance = (T)this;
    }
}