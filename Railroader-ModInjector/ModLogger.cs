using System;
using Serilog;
using Serilog.Core;

namespace Railroader.ModInjector;

internal static class ModLogger
{
    public static ILogger ForContext<T>() => ForContext(typeof(T));

    public static ILogger ForContext(Type type) {
        if (Log.Logger == Logger.None) {
            throw new Exception("Logger not configured yet. This method cannot be called in static context.");
        }

        return Log.ForContext(type);
    }
}
