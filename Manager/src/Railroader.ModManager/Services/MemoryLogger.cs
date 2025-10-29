using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Railroader.ModManager.Services;

internal interface IMemoryLogger : ILogger;

/// <summary> Initial logger - used before real logger is configured </summary>
[ExcludeFromCodeCoverage] // Ignored by coverage as code is basically wrapper
internal sealed class MemoryLogger : IMemoryLogger
{
    private record LogMessage(LogEventLevel Level, Exception Exception, string MessageTemplate, params object?[] PropertyValues);

    private readonly List<object> _LogMessages = new();

    /// <summary> FLush messages from this logger to real logger. </summary>
    /// <param name="logger">Real logger instance.</param>
    public void Flush(ILogger logger) {
        foreach (var message in _LogMessages) {
            FlushMessage(message, logger);
        }

        logger.Information("All log messages before this one were logged before the logger was configured. Their timestamps may be invalid.");
    }

    private static void FlushMessage(object message, ILogger logger) {
        switch (message) {
            case LogEvent logEvent: logger.Write(logEvent); break;
            case LogMessage simple: logger.Write(simple.Level, simple.Exception, simple.MessageTemplate, simple.PropertyValues); break;
        }
    }

    public ILogger ForContext(ILogEventEnricher enricher) => this;
    public ILogger ForContext(IEnumerable<ILogEventEnricher> enrichers) => this;
    public ILogger ForContext(string propertyName, object value, bool destructureObjects = false) => this;
    public ILogger ForContext<TSource>() => this;
    public ILogger ForContext(Type source) => this;

    public void Write(LogEvent logEvent) => _LogMessages.Add(logEvent);

    public void Write(LogEventLevel level, string messageTemplate) => Write(level, null!, messageTemplate, []);
    public void Write<T>(LogEventLevel level, string messageTemplate, T propertyValue) => Write(level, null!, messageTemplate, [propertyValue]);
    public void Write<T0, T1>(LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(level, null!, messageTemplate, [propertyValue0, propertyValue1]);
    public void Write<T0, T1, T2>(LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(level, null!, messageTemplate, [propertyValue0, propertyValue1, propertyValue2]);
    public void Write(LogEventLevel level, string messageTemplate, params object?[] propertyValues) => Write(level, (Exception)null!, messageTemplate, propertyValues);
    public void Write(LogEventLevel level, Exception exception, string messageTemplate) => Write(level, exception, messageTemplate, []);
    public void Write<T>(LogEventLevel level, Exception exception, string messageTemplate, T propertyValue) => Write(level, exception, messageTemplate, [propertyValue]);
    public void Write<T0, T1>(LogEventLevel level, Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(level, exception, messageTemplate, [propertyValue0, propertyValue1]);
    public void Write<T0, T1, T2>(LogEventLevel level, Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(level, exception, messageTemplate, [propertyValue0, propertyValue1, propertyValue2]);
    public void Write(LogEventLevel level, Exception exception, string messageTemplate, params object?[] propertyValues) => _LogMessages.Add(new LogMessage(level, exception, messageTemplate, propertyValues));

    public bool IsEnabled(LogEventLevel level) => true;

    public void Verbose(string messageTemplate) => Write(LogEventLevel.Verbose, messageTemplate);
    public void Verbose<T>(string messageTemplate, T propertyValue) => Write(LogEventLevel.Verbose, messageTemplate, propertyValue);
    public void Verbose<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Verbose, messageTemplate, propertyValue0, propertyValue1);
    public void Verbose<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Verbose, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Verbose(string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Verbose, messageTemplate, propertyValues);
    public void Verbose(Exception exception, string messageTemplate) => Write(LogEventLevel.Verbose, exception, messageTemplate);
    public void Verbose<T>(Exception exception, string messageTemplate, T propertyValue) => Write(LogEventLevel.Verbose, exception, messageTemplate, propertyValue);
    public void Verbose<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Verbose, exception, messageTemplate, propertyValue0, propertyValue1);
    public void Verbose<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Verbose, exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Verbose(Exception exception, string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Verbose, exception, messageTemplate, propertyValues);

    public void Debug(string messageTemplate) => Write(LogEventLevel.Debug, messageTemplate);
    public void Debug<T>(string messageTemplate, T propertyValue) => Write(LogEventLevel.Debug, messageTemplate, propertyValue);
    public void Debug<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Debug, messageTemplate, propertyValue0, propertyValue1);
    public void Debug<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Debug, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Debug(string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Debug, messageTemplate, propertyValues);
    public void Debug(Exception exception, string messageTemplate) => Write(LogEventLevel.Debug, exception, messageTemplate);
    public void Debug<T>(Exception exception, string messageTemplate, T propertyValue) => Write(LogEventLevel.Debug, exception, messageTemplate, propertyValue);
    public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Debug, exception, messageTemplate, propertyValue0, propertyValue1);
    public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Debug, exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Debug(Exception exception, string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Debug, exception, messageTemplate, propertyValues);

    public void Information(string messageTemplate) => Write(LogEventLevel.Information, messageTemplate);
    public void Information<T>(string messageTemplate, T propertyValue) => Write(LogEventLevel.Information, messageTemplate, propertyValue);
    public void Information<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Information, messageTemplate, propertyValue0, propertyValue1);
    public void Information<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Information, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Information(string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Information, messageTemplate, propertyValues);
    public void Information(Exception exception, string messageTemplate) => Write(LogEventLevel.Information, exception, messageTemplate);
    public void Information<T>(Exception exception, string messageTemplate, T propertyValue) => Write(LogEventLevel.Information, exception, messageTemplate, propertyValue);
    public void Information<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Information, exception, messageTemplate, propertyValue0, propertyValue1);
    public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Information, exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Information(Exception exception, string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Information, exception, messageTemplate, propertyValues);

    public void Warning(string messageTemplate) => Write(LogEventLevel.Warning, messageTemplate);
    public void Warning<T>(string messageTemplate, T propertyValue) => Write(LogEventLevel.Warning, messageTemplate, propertyValue);
    public void Warning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Warning, messageTemplate, propertyValue0, propertyValue1);
    public void Warning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Warning, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Warning(string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Warning, messageTemplate, propertyValues);
    public void Warning(Exception exception, string messageTemplate) => Write(LogEventLevel.Warning, exception, messageTemplate);
    public void Warning<T>(Exception exception, string messageTemplate, T propertyValue) => Write(LogEventLevel.Warning, exception, messageTemplate, propertyValue);
    public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Warning, exception, messageTemplate, propertyValue0, propertyValue1);
    public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Warning, exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Warning(Exception exception, string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Warning, exception, messageTemplate, propertyValues);

    public void Error(string messageTemplate) => Write(LogEventLevel.Error, messageTemplate);
    public void Error<T>(string messageTemplate, T propertyValue) => Write(LogEventLevel.Error, messageTemplate, propertyValue);
    public void Error<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Error, messageTemplate, propertyValue0, propertyValue1);
    public void Error<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Error, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Error(string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Error, messageTemplate, propertyValues);
    public void Error(Exception exception, string messageTemplate) => Write(LogEventLevel.Error, exception, messageTemplate);
    public void Error<T>(Exception exception, string messageTemplate, T propertyValue) => Write(LogEventLevel.Error, exception, messageTemplate, propertyValue);
    public void Error<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Error, exception, messageTemplate, propertyValue0, propertyValue1);
    public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Error, exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Error(Exception exception, string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Error, exception, messageTemplate, propertyValues);

    public void Fatal(string messageTemplate) => Write(LogEventLevel.Fatal, messageTemplate);
    public void Fatal<T>(string messageTemplate, T propertyValue) => Write(LogEventLevel.Fatal, messageTemplate, propertyValue);
    public void Fatal<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Fatal, messageTemplate, propertyValue0, propertyValue1);
    public void Fatal<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Fatal, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Fatal(string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Fatal, messageTemplate, propertyValues);
    public void Fatal(Exception exception, string messageTemplate) => Write(LogEventLevel.Fatal, exception, messageTemplate);
    public void Fatal<T>(Exception exception, string messageTemplate, T propertyValue) => Write(LogEventLevel.Fatal, exception, messageTemplate, propertyValue);
    public void Fatal<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) => Write(LogEventLevel.Fatal, exception, messageTemplate, propertyValue0, propertyValue1);
    public void Fatal<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => Write(LogEventLevel.Fatal, exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void Fatal(Exception exception, string messageTemplate, params object[] propertyValues) => Write(LogEventLevel.Fatal, exception, messageTemplate, propertyValues);

    [MessageTemplateFormatMethod("messageTemplate")]
    public bool BindMessageTemplate(string messageTemplate, object[] propertyValues, out MessageTemplate? parsedTemplate, out IEnumerable<LogEventProperty>? boundProperties) => throw new NotSupportedException();
    public bool BindProperty(string propertyName, object value, bool destructureObjects, out LogEventProperty? property) => throw new NotSupportedException();
}
