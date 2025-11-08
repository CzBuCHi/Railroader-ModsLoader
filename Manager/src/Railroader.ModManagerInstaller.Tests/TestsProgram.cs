using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Railroader.ModManagerInstaller.Abstractions;
using Railroader.ModManagerInstaller.Tests.Utils;
using Shouldly;

namespace Railroader.ModManagerInstaller.Tests;

[Collection("ModManagerInstaller")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public sealed class TestsProgram
{
    [Fact]
    public void MainReports_InstallerException() {
        // Arrange
        TestHelper.PrepareAppServices();
        AppServices.Assembly.GetExecutingAssembly().Throws(new InstallerException("Foo"));

        // Act
        Program.Main();

        // Assert
        Received.InOrder(() => {
            AppServices.Assembly.GetExecutingAssembly();
            AppServices.Console.WriteLine("Foo", ConsoleColor.Red);
            AppServices.Console.WriteLine("Press any key to exit.", ConsoleColor.White);
            AppServices.Console.ReadKey();
        });
    }

    [Fact]
    public void MainReports_InstallerException_WithInnerException() {
        // Arrange
        TestHelper.PrepareAppServices();
        AppServices.Assembly.GetExecutingAssembly().Throws(new InstallerException("Foo", new("Bar")));

        // Act
        Program.Main();

        // Assert
        Received.InOrder(() => {
            AppServices.Assembly.GetExecutingAssembly();
            AppServices.Console.WriteLine("Foo", ConsoleColor.Red);
            AppServices.Console.WriteLine(" - Bar", ConsoleColor.Red);
            AppServices.Console.WriteLine("Press any key to exit.", ConsoleColor.White);
            AppServices.Console.ReadKey();
        });
    }

    [Fact]
    public void MainReports_GamePathException() {
        // Arrange
        TestHelper.PrepareAppServices();
        AppServices.Assembly.GetExecutingAssembly().Throws(new GamePathException("Foo"));

        // Act
        Program.Main();

        // Assert
        Received.InOrder(() => {
            AppServices.Assembly.GetExecutingAssembly();
            AppServices.Console.WriteLine("Foo", ConsoleColor.Red);
            AppServices.Console.WriteLine("Could not determine Railroader directory automatically.", ConsoleColor.Red);
            AppServices.Console.WriteLine("Move this installer into your game's directory, then run again.");
            AppServices.Console.WriteLine("Press any key to exit.", ConsoleColor.White);
            AppServices.Console.ReadKey();
        });
    }

    [Fact]
    public void MainReports_GenericsException() {
        // Arrange
        TestHelper.PrepareAppServices();
        AppServices.Assembly.GetExecutingAssembly().Throws(new InvalidOperationException("Foo"));

        // Act
        Program.Main();

        // Assert
        Received.InOrder(() => {
            AppServices.Assembly.GetExecutingAssembly();
            AppServices.Console.WriteLine("Unexpected error:", ConsoleColor.Red);
            AppServices.Console.WriteLine(Arg.Is<string>(o => o.StartsWith("System.InvalidOperationException: Foo")), ConsoleColor.Red);
            AppServices.Console.WriteLine("Press any key to exit.", ConsoleColor.White);
            AppServices.Console.ReadKey();
        });
    }
    
    [Fact]
    public void MainReports_Success() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");

        const string gamePath = @"D:\SteamLibrary\steamapps\common\Railroader";
        var exePath = Path.Combine(gamePath, "Railroader.exe");

        GameDirectoryResolver.TryResolveGameDirectory = _ => gamePath;
        AppServices.File.Exists(exePath).Returns(true);

        ResourceExtractor.ExtractFiles = Substitute.For<Action<IAssembly>>();
        Patcher.PatchGame = Substitute.For<Action>();

        // Act
        Program.Main();

        Received.InOrder(() => {
            // Title + version
            AppServices.Console.Write("Installer ");
            AppServices.Console.WriteLine(new Version(1, 2, 3), ConsoleColor.DarkGreen);
            AppServices.Console.SetTitle("Installer 1.2.3");

            // Game directory
            GameDirectoryResolver.TryResolveGameDirectory(executingAssembly);
            AppServices.File.Exists(exePath);
            AppServices.Console.WriteLine("Found Railroader using Steam's Library.");
            AppServices.Directory.SetCurrentDirectory(gamePath);

            ResourceExtractor.ExtractFiles(executingAssembly);
            Patcher.PatchGame();
            AppServices.Directory.CreateDirectory("Mods");
            AppServices.Console.WriteLine("Installation complete!", ConsoleColor.DarkGreen);
            AppServices.Console.WriteLine("Press any key to exit.", ConsoleColor.White);
            AppServices.Console.ReadKey();
        });
    }

    [Fact]
    public void ReportInstallerVersion_AndSetConsoleTitle() {
        // Arrange
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<TestSuicide>();

        // Act
        var act = Program.RunInstaller;

        // Assert
        act.ShouldThrow<TestSuicide>();

        // Assert
        AppServices.Console.Received().Write("Installer ");
        AppServices.Console.Received().WriteLine(new Version(1, 2, 3), ConsoleColor.DarkGreen);
        AppServices.Console.Received().SetTitle("Installer 1.2.3");
    }

    [Fact]
    public void IgnoresPlatformNotSupportedException() {
        // Arrange
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();

        GameDirectoryResolver.TryResolveGameDirectory = Substitute.For<Func<IAssembly, string?>>();
        GameDirectoryResolver.TryResolveGameDirectory(Arg.Any<IAssembly>())!.Throws<TestSuicide>();

        // Act
        var act = Program.RunInstaller;

        // Assert
        act.ShouldThrow<TestSuicide>();

        // Assert
        AppServices.Console.Received().Write("Installer ");
        AppServices.Console.Received().WriteLine(new Version(1, 2, 3), ConsoleColor.DarkGreen);
        AppServices.Console.Received().SetTitle("Installer 1.2.3");
    }

    [Fact]
    public void ResolveGameDirectory_ResolverReturnsNull_ThrowsGamePathException() {
        // Arrange
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");

        // Skip title
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();

        GameDirectoryResolver.TryResolveGameDirectory = Substitute.For<Func<IAssembly, string?>>();
        GameDirectoryResolver.TryResolveGameDirectory(Arg.Any<IAssembly>()).Returns((string?)null);

        // Act
        var act = Program.RunInstaller;

        // Assert
        act.ShouldThrow<GamePathException>()
            .Message.ShouldBe("Could not find Railroader.exe using Steam's Library.");
    }

    [Fact]
    public void ResolveGameDirectory_ResolverReturnsNonExistingPath_ThrowsGamePathException() {
        // Arrange
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");

        // Skip title
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();

        GameDirectoryResolver.TryResolveGameDirectory = Substitute.For<Func<IAssembly, string?>>();
        GameDirectoryResolver.TryResolveGameDirectory(Arg.Any<IAssembly>()).Returns(@"C:\Invalid");

        // Act
        var act = Program.RunInstaller;

        // Assert
        act.ShouldThrow<GamePathException>()
            .Message.ShouldBe("Could not find Railroader.exe (Steam's Library path is invalid).");
    }

    [Fact]
    public void ResolveGameDirectory_ResolverReturnsExistingPath_SetCurrentDirectory() {
        // Arrange
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");

        GameDirectoryResolver.TryResolveGameDirectory = Substitute.For<Func<IAssembly, string?>>();
        GameDirectoryResolver.TryResolveGameDirectory(Arg.Any<IAssembly>()).Returns(@"C:\Game");
        AppServices.File.Exists(@"C:\Game\Railroader.exe").Returns(true);

        AppServices.Console.When(o => o.WriteLine("Extracting files ...")).Throw<TestSuicide>();

        // Act
        var act = Program.RunInstaller;

        // Assert
        act.ShouldThrow<TestSuicide>();

        Received.InOrder(() => {
            AppServices.Console.WriteLine("Found Railroader using Steam's Library.");
            AppServices.Directory.SetCurrentDirectory(@"C:\Game");
        });
    }

    [Fact]
    public void RunInstaller_SuccessfulInstallation_CompletesAllSteps() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");

        const string gamePath = @"D:\SteamLibrary\steamapps\common\Railroader";
        var exePath = Path.Combine(gamePath, "Railroader.exe");

        GameDirectoryResolver.TryResolveGameDirectory = _ => gamePath;
        AppServices.File.Exists(exePath).Returns(true);

        ResourceExtractor.ExtractFiles = Substitute.For<Action<IAssembly>>();
        Patcher.PatchGame = Substitute.For<Action>();

        // Act
        Program.RunInstaller();

        Received.InOrder(() => {
            // Title + version
            AppServices.Console.Write("Installer ");
            AppServices.Console.WriteLine(new Version(1, 2, 3), ConsoleColor.DarkGreen);
            AppServices.Console.SetTitle("Installer 1.2.3");

            // Game directory
            GameDirectoryResolver.TryResolveGameDirectory(executingAssembly);
            AppServices.File.Exists(exePath);
            AppServices.Console.WriteLine("Found Railroader using Steam's Library.");
            AppServices.Directory.SetCurrentDirectory(gamePath);

            ResourceExtractor.ExtractFiles(executingAssembly);
            Patcher.PatchGame();
            AppServices.Directory.CreateDirectory("Mods");
            AppServices.Console.WriteLine("Installation complete!", ConsoleColor.DarkGreen);
        });
    }

    [Fact]
    public void ResolveInternalAssemblies_UnknownName() {
        // Arrange
        TestHelper.PrepareAppServices();
        
        // Act
        var act = () => Program.ResolveInternalAssemblies(AppDomain.CurrentDomain, new("Name"));

        // Assert
        act.ShouldThrow<InstallerException>()
            .Message.ShouldBe("Could not load missing assembly: Name");
    }
    
    [Fact]
    public void ResolveInternalAssemblies_NotEmbedded() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");
        
        // Act
        var act = () => Program.ResolveInternalAssemblies(AppDomain.CurrentDomain, new("Mono.Cecil"));

        // Assert
        act.ShouldThrow<InstallerException>()
            .Message.ShouldBe("Embedded assembly not found: Mono.Cecil.dll");

        executingAssembly.Received().GetManifestResourceStream("Assemblies/Mono.Cecil.dll");
    }
    
    [Fact]
    public void ResolveInternalAssemblies_Embedded() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");
        byte[] buffer = [1, 2, 3];
        var stream = new MemoryStream(buffer);
        executingAssembly.GetManifestResourceStream("Assemblies/Mono.Cecil.dll").Returns(stream);
        var assembly = Substitute.For<IAssembly>();
        AppServices.Assembly.Load(Arg.Is<byte[]>(o => o.SequenceEqual(buffer))).Returns(_ => assembly);
        
        // Act
        var actual = Program.ResolveInternalAssemblies(AppDomain.CurrentDomain, new("Mono.Cecil"));

        // Assert
        actual.ShouldBe(assembly);
        executingAssembly.Received().GetManifestResourceStream("Assemblies/Mono.Cecil.dll");
    }
}
