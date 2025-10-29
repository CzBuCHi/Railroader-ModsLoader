using Serilog;

namespace Railroader.ModManager.Services;

internal interface ILoggerFactory
{
    ILogger GetLogger(string? scope = null);
}

/// <inheritdoc />
internal class LoggerFactory(ILogger logger) : ILoggerFactory
{
    /// <inheritdoc />
    public ILogger GetLogger(string? scope = null) => logger.ForContext("SourceContext", scope ?? "Railroader.ModInjector");
}
