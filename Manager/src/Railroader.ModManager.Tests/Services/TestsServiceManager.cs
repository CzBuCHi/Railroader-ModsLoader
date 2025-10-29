using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Services;

namespace Railroader.ModManager.Tests.Services;

public sealed class ServiceManagerTests
{
    [Fact]
    public void AddSingleton_SuccessfullyRegisters() {
        // Arrange
        var sut = new ServiceManager();

        // Act
        sut.AddSingleton<IFoo, Foo>();

        // Assert
        var context = sut.Services.Should().ContainKey(typeof(IFoo)).WhoseValue;
        context.IsSingleton.Should().BeTrue();
        context.Instance.Should().BeNull();
        context.Factory.Should().NotBeNull();
    }

    [Fact]
    public void AddTransient_SuccessfullyRegisters() {
        // Arrange
        var sut = new ServiceManager();

        // Act
        sut.AddTransient<IFoo, Foo>();

        // Assert
        var context = sut.Services.Should().ContainKey(typeof(IFoo)).WhoseValue;
        context.IsSingleton.Should().BeFalse();
        context.Instance.Should().BeNull();
        context.Factory.Should().NotBeNull();
    }

    [Fact]
    public void AddSingleton_Factory_SuccessfullyRegisters() {
        // Arrange
        var sut      = new ServiceManager();
        var instance = new Foo();

        // Act
        sut.AddSingleton<IFoo, Foo>(_ => instance);

        // Assert
        var context = sut.Services.Should().ContainKey(typeof(IFoo)).WhoseValue;
        context.IsSingleton.Should().BeTrue();
        context.Instance.Should().BeNull();
        context.Factory(sut).Should().BeSameAs(instance);
    }

    [Fact]
    public void AddTransient_Factory_SuccessfullyRegisters() {
        // Arrange
        var sut      = new ServiceManager();
        var instance = new Foo();

        // Act
        sut.AddTransient<IFoo, Foo>(_ => instance);

        // Assert
        var context = sut.Services.Should().ContainKey(typeof(IFoo)).WhoseValue;
        context.IsSingleton.Should().BeFalse();
        context.Instance.Should().BeNull();
        context.Factory(sut).Should().BeSameAs(instance);
    }

    [Fact]
    public void Add_Duplicate_Throws() {
        // Arrange
        var sut = new ServiceManager();
        sut.AddSingleton<IFoo, Foo>();

        // Act & Assert
        var act = () => sut.AddSingleton<IFoo, Foo>([ExcludeFromCodeCoverage](_) => new Foo());
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Service {typeof(IFoo)} is already registered");
    }

    [Fact]
    public void Get_Singleton_ReturnsSameInstance() {
        // Arrange
        var sut = new ServiceManager();
        sut.AddSingleton<IFoo, Foo>();

        // Act
        var first  = sut.GetService(typeof(IFoo));
        var second = sut.GetService(typeof(IFoo));

        // Assert
        first.Should().BeOfType<Foo>();
        second.Should().BeSameAs(first);
        var context = sut.Services.Should().ContainKey(typeof(IFoo)).WhoseValue;
        context.Instance.Should().BeSameAs(first);
    }

    [Fact]
    public void Get_Transient_ReturnsNewInstances() {
        // Arrange
        var sut = new ServiceManager();
        sut.AddTransient<IFoo, Foo>();

        // Act
        var first  = sut.GetService(typeof(IFoo));
        var second = sut.GetService(typeof(IFoo));

        // Assert
        first.Should().BeOfType<Foo>();
        second.Should().BeOfType<Foo>();
        second.Should().NotBeSameAs(first);
        var context = sut.Services.Should().ContainKey(typeof(IFoo)).WhoseValue;
        context.Instance.Should().BeNull();
    }

    [Fact]
    public void Get_UnregisteredService_Throws() {
        // Arrange
        var sut = new ServiceManager();

        // Act & Assert
        Action act = () => sut.GetService<IFoo>();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Cannot find service {typeof(IFoo)}");
    }

    [Fact]
    public void Get_NullFactoryResult_Throws() {
        // Arrange
        var sut = new ServiceManager();
        sut.AddTransient<IFoo, Foo>(_ => null!);

        // Act & Assert
        Action act = () => sut.GetService<IFoo>();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Failed to create instance of {typeof(IFoo)}");
    }

    private interface IFoo;

    private sealed class Foo : IFoo;
}
