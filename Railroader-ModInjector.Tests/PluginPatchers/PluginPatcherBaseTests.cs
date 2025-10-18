using System.IO;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using Railroader.ModInjector.PluginPatchers;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader_ModInterfaces.Tests.PluginPatchers;

public class PluginPatcherBaseTests
{
    [Fact]
    public void SkiIfNotImplementMarkerInterface() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var sut    = new TestPluginPatcher(logger);

        const string source = """
                              using Railroader.ModInterfaces;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                              }
                              """;

        var outputPath         = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "PluginPatcherBaseTests", "SkiIfNotImplementMarkerInterface");
        var assemblyDefinition = AssemblyTestUtils.BuildAssemblyDefinition(source, outputPath);
        var typeDefinition     = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.FirstPlugin");

        // Act
        sut.Patch(assemblyDefinition, typeDefinition);

        // Assert
        logger.Received().Debug("Skipping patching for type {TypeName}: not derived from PluginBase or does not implement {PluginInterface}", typeDefinition.FullName, typeof(IHarmonyPlugin).FullName);
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public void CreateOnIsEnabledChangedOverrideIfNotFound() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var sut    = new TestPluginPatcher(logger);

        const string source = """
                              using Railroader.ModInterfaces;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public sealed class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
                                  {
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                      }
                                  }
                              }
                              """;

        var outputPath         = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "PluginPatcherBaseTests", "CreateOnIsEnabledChangedOverrideIfNotFound");
        var assemblyDefinition = AssemblyTestUtils.BuildAssemblyDefinition(source, outputPath);
        var typeDefinition     = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.FirstPlugin");

        // Act
        sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        logger.Received().Debug("OnIsEnabledChanged method not found in {TypeName}, creating override", typeDefinition.FullName);
        logger.Received().Debug("Created OnIsEnabledChanged override with base call in {TypeName}", typeDefinition.FullName);
        logger.Received().Information("Successfully patched OnIsEnabledChanged in {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IHarmonyPlugin).FullName);
        logger.ReceivedCalls().Should().HaveCount(3);
    }

    [Fact]
    public void InjectToExisting() {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var sut    = new TestPluginPatcher(logger);

        const string source = """
                              using Railroader.ModInterfaces;
                              using Serilog;

                              namespace Foo.Bar
                              {
                                  public class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
                                  {
                                      public ILogger Logger { get; }
                                  
                                      public FirstPlugin(IModdingContext moddingContext, IMod mod) 
                                          : base(moddingContext, mod) {
                                          Logger = mod.CreateLogger();
                                      }
                                      
                                      protected override void OnIsEnabledChanged() {
                                          base.OnIsEnabledChanged();
                                          Logger.Information("OnIsEnabledChanged: " + IsEnabled);
                                      }
                                  }

                              }
                              """;

        var outputPath         = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "PluginPatcherBaseTests", "SealedClass");
        var assemblyDefinition = AssemblyTestUtils.BuildAssemblyDefinition(source, outputPath);
        var typeDefinition     = assemblyDefinition.MainModule!.Types!.First(o => o.FullName == "Foo.Bar.FirstPlugin");

        // Act
        sut.Patch(assemblyDefinition, typeDefinition);
        //assemblyDefinition.Write(Path.Combine(outputPath, "patched.dll"));

        // Assert
        logger.Received().Information("Successfully patched OnIsEnabledChanged in {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(IHarmonyPlugin).FullName);
        logger.ReceivedCalls().Should().HaveCount(1);
    }

    public sealed class TestPluginPatcher(ILogger logger) : PluginPatcherBase<IHarmonyPlugin, TestPluginPatcher>(logger)
    {
        [UsedImplicitly]
        public static void OnIsEnabledChanged(IPluginBase plugin) {
        }
    }
}

