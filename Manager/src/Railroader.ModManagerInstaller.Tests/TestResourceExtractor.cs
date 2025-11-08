using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NSubstitute;
using Railroader.ModManagerInstaller.Abstractions;
using Railroader.ModManagerInstaller.Tests.Utils;
using Shouldly;

namespace Railroader.ModManagerInstaller.Tests;

[Collection("ModManagerInstaller")]
public sealed class TestResourceExtractor
{
    private IEnumerable<(string resourceName, string filePath, byte[] buffer, MemoryStream fileStream)> PrepareData(IAssembly executingAssembly) {
        const string prefix = "Railroader.ModManagerInstaller.Assemblies";

        string[] assemblies = [
            "0Harmony.dll",
            "Mono.Cecil.dll",
            "Mono.CSharp.dll",
            "Railroader.ModManager.dll",
            "Railroader.ModManager.Interfaces.dll"
        ];

        return assemblies.Select(name => {
            var resourceName = $"{prefix}.{name}";
            var filePath = @"C:\Game\Railroader_Data\Managed\" + name;
            var buffer = Encoding.UTF8.GetBytes(name);
            var resourceStream = new MemoryStream(buffer);
            var fileStream = new MemoryStream();

            executingAssembly.GetManifestResourceStream(resourceName).Returns(resourceStream);
            AppServices.File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write).Returns(fileStream);

            return (resourceName, filePath, buffer, fileStream);
        });
    }

    [Fact]
    public void ExtractAssembly() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");

        var data = PrepareData(executingAssembly).ToArray();

        // Act
        ResourceExtractor.ExtractFiles(executingAssembly);

        // Assert
        foreach (var (resourceName, filePath, buffer, fileStream) in data) {
            AppServices.Console.Received().WriteLine(filePath, ConsoleColor.DarkCyan);
            executingAssembly.Received().GetManifestResourceStream(resourceName);
            AppServices.File.Received().Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
            fileStream.ToArray().ShouldBe(buffer);
        }

        executingAssembly.Received(data.Length).GetManifestResourceStream(Arg.Any<string>());
    }
}
