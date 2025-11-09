using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Railroader.ModManagerInstaller.Abstractions;
using Railroader.ModManagerInstaller.Tests.Utils;
using Shouldly;

namespace Railroader.ModManagerInstaller.Tests;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
[Collection("ModManagerInstaller")]
public sealed class TestGameDirectoryResolver
{
    private const string GameDir = @"C:\Game";
    private const string RailroaderPath = @"C:\Game\Railroader.exe";

    private static void PrepareAppServices(object? steamPath) {
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");
        AppServices.Directory.GetCurrentDirectory().Returns(@"C:\Temp");
        AppServices.File.Exists(Arg.Is<string>(p => p.EndsWith("Railroader.exe"))).Returns(false);

        if (steamPath == null) {
            AppServices.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam").Returns((IRegistryKey?)null);
        } else {
            var steamKey = Substitute.For<IRegistryKey>();
            steamKey.GetValue("SteamPath").Returns(steamPath);
            AppServices.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam").Returns(steamKey);
        }
    }

    private static void PrepareSteamPath(string steamPath, bool pathValid) {
        PrepareAppServices(steamPath);
        AppServices.Directory.Exists(steamPath).Returns(pathValid);
    }

    private static void PrepareVdf(VdfEntry entry) {
        PrepareSteamPath(@"C:\Valid", true);
        
        VdfEntry.Load = Substitute.For<VdfEntryLoader>();
        VdfEntry.Load.Invoke(@"C:\Valid\steamapps\libraryfolders.vdf").Returns(entry);
    }

    [Fact]
    public void TryResolveGameDirectory_CurrentDirectory() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns(GameDir);
        AppServices.File.Exists(RailroaderPath).Returns(true);

        // Act
        var actual = GameDirectoryResolver.TryResolveGameDirectory(executingAssembly);
        
        // Assert
        actual.ShouldBe(GameDir);

        Received.InOrder(() =>
        {
            AppServices.Directory.GetCurrentDirectory();
            AppServices.File.Exists(RailroaderPath);
            AppServices.Console.WriteLine("Found Railroader in the current working directory.");
        });
    }
    
    [Fact]
    public void TryResolveGameDirectory_ExecutingAssemblyLocation() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns(@"C:\Temp");
        AppServices.File.Exists(@"C:\Temp\Railroader.exe").Returns(false);
        AppServices.File.Exists(RailroaderPath).Returns(true);

        // Act
        var actual = GameDirectoryResolver.TryResolveGameDirectory(executingAssembly);
        
        // Assert
        actual.ShouldBe(GameDir);

        Received.InOrder(() =>
        {
            AppServices.Directory.GetCurrentDirectory();
            AppServices.File.Exists(@"C:\Temp\Railroader.exe");
            AppServices.File.Exists(RailroaderPath);
            AppServices.Console.WriteLine("Found Railroader in the Installer assembly directory.");
        });
    }
    
    [Fact]
    public void TryResolveGameDirectory_SteamPath() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\NotGame\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns("C:\\Temp");
        AppServices.File.Exists(@"C:\Temp\Railroader.exe").Returns(false);
        AppServices.File.Exists(@"C:\NotGame\Railroader.exe").Returns(false);
        
        var entry = new VdfEntry {
            {
                "libraryfolders", new VdfEntry {
                    {
                        "0", new VdfEntry {
                            { "path", "C:\\SteamRoot" }, 
                            {

                                "apps", new VdfEntry {
                                    { "0", "0" },
                                }
                            }
                        }
                    }
                }
            }
        };
        PrepareVdf(entry);
        
        // Act
        var actual = GameDirectoryResolver.TryResolveGameDirectory(executingAssembly);
        
        // Assert
        actual.ShouldBeNull();

        Received.InOrder(() =>
        {
            AppServices.Directory.GetCurrentDirectory();
            AppServices.File.Exists(@"C:\Temp\Railroader.exe");
            AppServices.File.Exists(@"C:\NotGame\Railroader.exe");
            AppServices.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            VdfEntry.Load(@"C:\Valid\steamapps\libraryfolders.vdf");
        });
    }
    
    [Fact]
    public void TryResolveGameDirectory_Null() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\NotGame\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns("C:\\Temp");
        AppServices.File.Exists(@"C:\Temp\Railroader.exe").Returns(false);
        AppServices.File.Exists(@"C:\NotGame\Railroader.exe").Returns(false);
        
        var entry = new VdfEntry();
        PrepareVdf(entry);
        
        // Act
        var actual = GameDirectoryResolver.TryResolveGameDirectory(executingAssembly);
        
        // Assert
        actual.ShouldBeNull();

        Received.InOrder(() =>
        {
            AppServices.Directory.GetCurrentDirectory();
            AppServices.File.Exists(@"C:\Temp\Railroader.exe");
            AppServices.File.Exists(@"C:\NotGame\Railroader.exe");
            AppServices.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            VdfEntry.Load(@"C:\Valid\steamapps\libraryfolders.vdf");
        });
    }

    [Fact]
    public void CheckCurrentDirectory_Succeed()
    {
        // Arrange
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns(GameDir);
        AppServices.File.Exists(RailroaderPath).Returns(true);
        
        // Act
        var actual = GameDirectoryResolver.CheckCurrentDirectory();

        // Assert
        actual.ShouldBe(GameDir);

        Received.InOrder(() =>
        {
            AppServices.Directory.GetCurrentDirectory();
            AppServices.File.Exists(RailroaderPath);
            AppServices.Console.WriteLine("Found Railroader in the current working directory.");
        });
    }
    
    [Fact]
    public void CheckCurrentDirectory_Fails()
    {
        // Arrange
        TestHelper.PrepareAppServices();
        TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns(GameDir);
        AppServices.File.Exists(RailroaderPath).Returns(false);
        
        // Act
        var actual = GameDirectoryResolver.CheckCurrentDirectory();

        // Assert
        actual.ShouldBeNull();

        Received.InOrder(() =>
        {
            AppServices.Directory.GetCurrentDirectory();
            AppServices.File.Exists(RailroaderPath);
        });
    }

    [Fact]
    public void CheckExecutingAssemblyLocation_Succeed() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Game\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns("C:\\Temp");
        AppServices.File.Exists(RailroaderPath).Returns(true);
        
        // Act
        var actual = GameDirectoryResolver.CheckExecutingAssemblyLocation(executingAssembly);

        // Assert
        actual.ShouldBe(GameDir);

        Received.InOrder(() =>
        {
            AppServices.File.Exists(RailroaderPath);
            AppServices.Console.WriteLine("Found Railroader in the Installer assembly directory.");
        });
    }
    
    [Fact]
    public void CheckExecutingAssemblyLocation_Fails() {
        // Arrange
        TestHelper.PrepareAppServices();
        var executingAssembly = TestHelper.PrepareExecutingAssembly(@"C:\Temp\Installer.dll");
        AppServices.Console.When(o => o.SetTitle(Arg.Any<string>())).Throw<PlatformNotSupportedException>();
        
        AppServices.Directory.GetCurrentDirectory().Returns("C:\\Temp");
        AppServices.File.Exists(@"C:\Temp\Railroader.exe").Returns(false);
        
        // Act
        var actual = GameDirectoryResolver.CheckExecutingAssemblyLocation(executingAssembly);

        // Assert
        actual.ShouldBeNull();

        Received.InOrder(() =>
        {
            AppServices.File.Exists(@"C:\Temp\Railroader.exe");
        });
    }

    [Fact]
    public void ResolveGameDirectoryFromRegistry_RegistryNotFound_ThrowsCannotFindSteamRegistry() {
        // Arrange
        PrepareAppServices(null);

        // Act
        var act = GameDirectoryResolver.ResolveGameDirectoryFromRegistry;

        // Assert
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldBe("Cannot find Steam registry");
    }

    [Fact]
    public void ResolveGameDirectory_SteamPathValueIsNotString_ThrowsSteamPathNotFound()
    {
        // Arrange
        PrepareAppServices(123);

        // Act
        var act = GameDirectoryResolver.ResolveGameDirectoryFromRegistry;

        // Assert
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldBe("Steam path not found, or does not exist on file system");
    }

    [Fact]
    public void ResolveGameDirectory_SteamPathValueIsNotValidDirectoryPath_ThrowsSteamPathNotFound()
    {
        // Arrange
        PrepareSteamPath(@"C:\Invalid", false);
        
        // Act
        var act = GameDirectoryResolver.ResolveGameDirectoryFromRegistry;

        // Assert
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldBe("Steam path not found, or does not exist on file system");
    }

    [Fact]
    public void ResolveGameDirectory_FailedToParseVdfEntry()
    {
        // Arrange
        PrepareSteamPath(@"C:\Valid", true);
        VdfEntry.Load = Substitute.For<VdfEntryLoader>();
        VdfEntry.Load.Invoke(@"C:\Valid\steamapps\libraryfolders.vdf").Throws(new VdfException("VDF"));
        
        // Act
        var act = GameDirectoryResolver.ResolveGameDirectoryFromRegistry;

        // Assert
        act.ShouldThrow<VdfException>()
            .Message.ShouldBe("VDF");
    }
    
    [Fact]
    public void ResolveGameDirectory_EmptyVdf()
    {
        // Arrange
        var entry = new VdfEntry();
        PrepareVdf(entry);
        
        // Act
        var result = GameDirectoryResolver.ResolveGameDirectoryFromRegistry();

        // Assert
        result.ShouldBeNull();
    }
    
    [Fact]
    public void ResolveGameDirectory_InvalidVdf_NoLibraryFolders()
    {
        // Arrange
        var entry = new VdfEntry {
            { "libraryfolders", new VdfEntry() }
        };
        PrepareVdf(entry);
        
        // Act
        var result = GameDirectoryResolver.ResolveGameDirectoryFromRegistry();

        // Assert
        result.ShouldBeNull();
    }
    
    [Fact]
    public void ResolveGameDirectory_InvalidVdf_NoAppsInLibraryFolders()
    {
        // Arrange
        var entry = new VdfEntry {
            {
                "libraryfolders", new VdfEntry {
                    { "0", new VdfEntry() }
                }
            }
        };
        PrepareVdf(entry);
        
        // Act
        var result = GameDirectoryResolver.ResolveGameDirectoryFromRegistry();

        // Assert
        result.ShouldBeNull();
    }
    
    [Fact]
    public void ResolveGameDirectory_InvalidVdf_NoRailroaderIdInApps()
    {
        // Arrange
        var entry = new VdfEntry {
            {
                "libraryfolders", new VdfEntry {
                    {
                        "0", new VdfEntry {
                            {
                                "apps", new VdfEntry {
                                    { "0", "0" },
                                    { "1", "1" },
                                }
                            }
                        }
                    }
                }
            }
        };
        PrepareVdf(entry);
        
        // Act
        var result = GameDirectoryResolver.ResolveGameDirectoryFromRegistry();

        // Assert
        result.ShouldBeNull();
    }
    
    
    [Fact]
    public void ResolveGameDirectory_InvalidVdf_NoPathInApps()
    {
        // Arrange
        var entry = new VdfEntry {
            {
                "libraryfolders", new VdfEntry {
                    {
                        "0", new VdfEntry {
                            {
                                "apps", new VdfEntry {
                                    { "1683150", "0" },
                                }
                            }
                        }
                    }
                }
            }
        };
        PrepareVdf(entry);
        
        // Act
        var act = GameDirectoryResolver.ResolveGameDirectoryFromRegistry;

        // Assert
        act.ShouldThrow<VdfException>()
            .Message.ShouldBe("Path not found");
    }
    
    [Fact]
    public void ResolveGameDirectory_ReturnsPathFromVdf()
    {
        // Arrange
        var entry = new VdfEntry {
            {
                "libraryfolders", new VdfEntry {
                    {
                        "0", new VdfEntry {
                            { "path", "C:\\SteamRoot" }, 
                            {

                                "apps", new VdfEntry {
                                    { "1683150", "0" },
                                }
                            }
                        }
                    }
                }
            }
        };
        PrepareVdf(entry);
        
        // Act
        var actual = GameDirectoryResolver.ResolveGameDirectoryFromRegistry();

        // Assert
        actual.ShouldBe(@"C:\SteamRoot\steamapps\common\Railroader");
    }

}
