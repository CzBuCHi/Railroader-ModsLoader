using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Logging;
using Railroader.ModManager.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Railroader.ModManager.Tests.Services;

public sealed class TestsLogConfigurator
{
    [Fact]
    public void ConfigureLoggerCorrectly() {
        // Arrange
        var configuration = new LoggerConfiguration().WriteTo.UnityConsole()!;
        var accessor = new LoggerConfigurationAccessor(configuration);
        ModDefinition[] definitions = [
            new() {
                Identifier = "NoLog",
                Name = "No log level",
                BasePath = @"Mods\DummyMod\"
            },
            new() {
                Identifier = "DefaultLog",
                Name = "Default log level",
                BasePath = @"Mods\DummyMod\",
                LogLevel = LogEventLevel.Information
            },
            new() {
                Identifier = "CustomLog",
                Name = "Custom log level",
                BasePath = @"Mods\DummyMod\",
                LogLevel = LogEventLevel.Fatal
            }
        ];

        var sut = new LogConfigurator();

        // Act
        sut.ConfigureLogger(configuration, definitions);

        // Assert
        accessor.Overrides.Should().ContainKey("Railroader.ModInjector").WhoseValue.MinimumLevel.Should().Be(LogEventLevel.Debug);
        accessor.Overrides.Should().ContainKey("CustomLog").WhoseValue.MinimumLevel.Should().Be(LogEventLevel.Fatal);

        accessor.LogEventSinks.Should().HaveCount(2);

        var conditionalSink = typeof(ILogger).Assembly.GetType("Serilog.Core.Sinks.ConditionalSink")!;
        conditionalSink.Should().NotBeNull();
        var wrapped = conditionalSink.GetField("_wrapped", BindingFlags.Instance | BindingFlags.NonPublic);
        wrapped.Should().NotBeNull();
        var condition = conditionalSink.GetField("_condition", BindingFlags.Instance | BindingFlags.NonPublic);
        condition.Should().NotBeNull();
        var formatter = typeof(SerilogUnityConsoleEventSink).GetField("_formatter", BindingFlags.Instance | BindingFlags.NonPublic);
        formatter.Should().NotBeNull();
        var outputTemplate = typeof(MessageTemplateTextFormatter).GetField("_outputTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
        outputTemplate.Should().NotBeNull();

        accessor.LogEventSinks.Should().AllBeOfType(conditionalSink);

        var eventWithContext    = new LogEvent(DateTimeOffset.Now, LogEventLevel.Debug, null!, new MessageTemplate("template", []), [new LogEventProperty("SourceContext", new ScalarValue("Value"))]);
        var eventWithoutContext = new LogEvent(DateTimeOffset.Now, LogEventLevel.Debug, null!, new MessageTemplate("template", []), []);

        var condition1 = (Func<LogEvent, bool>)condition.GetValue(accessor.LogEventSinks[0]!)!;
        condition1(eventWithContext).Should().BeTrue();
        condition1(eventWithoutContext).Should().BeFalse();

        var condition2 = (Func<LogEvent, bool>)condition.GetValue(accessor.LogEventSinks[1]!)!;
        condition2(eventWithContext).Should().BeFalse();
        condition2(eventWithoutContext).Should().BeTrue();

        var unitySink0 = wrapped.GetValue(accessor.LogEventSinks[0]!).Should().BeOfType<SerilogUnityConsoleEventSink>().Which;
        var unitySink1 = wrapped.GetValue(accessor.LogEventSinks[1]!).Should().BeOfType<SerilogUnityConsoleEventSink>().Which;

        var formatter0 = formatter.GetValue(unitySink0).Should().BeOfType<MessageTemplateTextFormatter>().Which;
        var formatter1 = formatter.GetValue(unitySink1).Should().BeOfType<MessageTemplateTextFormatter>().Which;

        var outputTemplate0 = outputTemplate.GetValue(formatter0).Should().BeOfType<MessageTemplate>().Which;
        var outputTemplate1 = outputTemplate.GetValue(formatter1).Should().BeOfType<MessageTemplate>().Which;

        outputTemplate0.Text.Should().Be("[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        outputTemplate1.Text.Should().Be("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
    }

    private sealed class LoggerConfigurationAccessor(LoggerConfiguration configuration)
    {
        private static readonly Type      _Type          = typeof(LoggerConfiguration);
        private static readonly FieldInfo _Overrides     = _Type.GetField("_overrides", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo _LogEventSinks = _Type.GetField("_logEventSinks", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public Dictionary<string, LoggingLevelSwitch> Overrides     => (Dictionary<string, LoggingLevelSwitch>)_Overrides.GetValue(configuration)!;
        public List<ILogEventSink>                    LogEventSinks => (List<ILogEventSink>)_LogEventSinks.GetValue(configuration)!;
    }
}
