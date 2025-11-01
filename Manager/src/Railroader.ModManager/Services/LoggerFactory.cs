using Serilog;

namespace Railroader.ModManager.Services;

public interface ILoggerFactory
{
    ILogger GetLogger(string? scope = null);
}

/// <inheritdoc />
public class LoggerFactory(ILogger logger) : ILoggerFactory
{
    /// <inheritdoc />
    public ILogger GetLogger(string? scope = null) => logger.ForContext("SourceContext", scope ?? "Railroader.ModInjector");
}
