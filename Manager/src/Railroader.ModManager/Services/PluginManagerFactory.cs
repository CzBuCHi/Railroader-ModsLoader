using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Services;

/// <summary> Factory for <see cref="IPluginManager"/> </summary>
internal interface IPluginManagerFactory
{
    /// <summary> Create new instance of <see cref="IPluginManager"/>. </summary>
    /// <param name="moddingContext"></param>
    /// <returns></returns>
    IPluginManager CreatePluginManager(IModdingContext moddingContext);
}

/// <inheritdoc />
internal sealed class PluginManagerFactory(ILogger logger) : IPluginManagerFactory
{
    /// <inheritdoc />
    public IPluginManager CreatePluginManager(IModdingContext moddingContext) => new PluginManager(moddingContext, logger);
}
