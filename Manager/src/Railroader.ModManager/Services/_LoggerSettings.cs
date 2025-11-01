using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Serilog.Events;

namespace Railroader.ModManager.Services;

[ExcludeFromCodeCoverage]
public sealed class LoggerSettings
{
    public Dictionary<string, LogEventLevel> ModsLogLevels { get; } = new() {
#if DEBUG
        { "", LogEventLevel.Debug }
#else
        { "", LogEventLevel.Information }
#endif
    };
}
