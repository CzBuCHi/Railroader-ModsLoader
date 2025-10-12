using System;
using System.IO;
using FluentAssertions;
using Railroader.ModInjector;
using Railroader.ModInterfaces;
using Xunit.Abstractions;

namespace Railroader_ModInterfaces.Tests;

[Collection("TestFixture")]
public sealed class HarmonyExporterTests(TestFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public void TestCodeCompiler() {
        // Arrange
        var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
        harmony.PatchAll(typeof(Injector).Assembly);

        var              assemblyCSharp = Path.Combine(Environment.CurrentDirectory, "Railroader_Data", "Managed", "Assembly-CSharp");

        IHarmonyExporter harmonyExporter = new HarmonyExporter();

        // Act
        harmonyExporter.ExportPatchedAssembly(assemblyCSharp + ".dll", harmony, assemblyCSharp + "_Patched.dll");

        // Assert
        output.WriteLine(fixture.LogMessages);
    }
}
