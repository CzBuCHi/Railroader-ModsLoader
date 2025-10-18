using System;
using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Railroader.ModInjector;
using Railroader.ModInjector.Services;
using Serilog;
using Serilog.Events;

namespace Railroader_ModInterfaces.Tests;

public class InjectorTests
{
    private static readonly ModDefinition[] _Definitions = [
        new() {
            Identifier = "First",
            Name = "Name",
            Version = new Version(1, 0),
            BasePath = @"\Current\Mods\First"
        },
        new() {
            Identifier = "Second",
            Name = "Name",
            Version = new Version(1, 0),
            LogLevel = LogEventLevel.Verbose,
            BasePath = @"\Current\Mods\Second"
        }
    ];

    [Fact]
    public void ModInjectorMain() {
        // Arrange
        var modManager     = Substitute.For<IModManager>();
        DI.ModManager = () => modManager;

        InjectorAccessor.SetModDefinitions(_Definitions);

        // Act
        Injector.ModInjectorMain();

        // Assert
        modManager.Received(1).Bootstrap(_Definitions);
    }

    [Fact]
    public void CreateLogger() {
        // Arrange
        

        var modDefinitionLoader = Substitute.For<IModDefinitionLoader>();
        modDefinitionLoader.LoadDefinitions().Returns(_Definitions);

        var logConfigurator = Substitute.For<ILogConfigurator>();
        var initLogger      = Substitute.For<IInitLogger>();
        var logger          = Substitute.For<ILogger>();
        var injectorLogger  = Substitute.For<ILogger>();

        DI.Logger = initLogger;
        DI.ModDefinitionLoader = () => modDefinitionLoader;
        DI.LogConfigurator = () => logConfigurator;
        DI.CreateLogger = _ => DI.Logger = logger;
        DI.GetLogger = _ => injectorLogger;

        var configuration = new LoggerConfiguration();

        // Act
        Injector.CreateLogger(configuration);

        // Assert
        logConfigurator.Received(1).ConfigureLogger(configuration, _Definitions);

        initLogger.Received().Flush(injectorLogger);

        logger.Information("Log level for {mod} set to {level}", "Second", LogEventLevel.Verbose);
    }

    private static class InjectorAccessor
    {
        private static readonly FieldInfo _ModDefinitions = typeof(Injector).GetField("_ModDefinitions", BindingFlags.Static | BindingFlags.NonPublic)!;

        public static void SetModDefinitions(ModDefinition[] definitions) => _ModDefinitions.SetValue(null!, definitions);
    }
}
