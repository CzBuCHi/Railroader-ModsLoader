using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;

namespace Railroader.ModManager.Interfaces.Tests;

public sealed class TestsPluginBase : IAsyncLifetime
{
    private IModdingContext _ModdingContext = null!;
    private IMod            _Mod            = null!;
    private TestPlugin      _Sut            = null!;

    public Task InitializeAsync() {
        _ModdingContext = Substitute.For<IModdingContext>();
        _Mod = Substitute.For<IMod>();
        _Sut = new TestPlugin(_ModdingContext, _Mod);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() {
        TestPlugin.Cleanup();
        return Task.CompletedTask;
    }

    [Fact]
    public void CallsOnEnableCorrectly() {
        // Arrange
        _Sut.SetIsEnabled(false);

        // Act
        _Sut.IsEnabled = true;
        _Sut.IsEnabled = true;

        // Assert
        _Sut.IsEnabled.ShouldBeTrue();
        _Sut.IsEnabledChanges.ShouldBeEquivalentTo(new List<bool> { true });
    }

    [Fact]
    public void CallsOnDisableCorrectly() {
        // Arrange
        _Sut.SetIsEnabled(true);

        // Act
        _Sut.IsEnabled = false;
        _Sut.IsEnabled = false;

        // Assert
        _Sut.IsEnabled.ShouldBeFalse();
        _Sut.IsEnabledChanges.ShouldBeEquivalentTo(new List<bool> { false });
    }

    [Fact]
    public void EnsureSingleton() {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => new TestPlugin(_ModdingContext, _Mod))
              .Message.ShouldBe($"Cannot create plugin '{typeof(TestPlugin)}' twice.");
    }

    [Fact]
    public void PrematureInstanceAccess() {
        // Arrange
        TestPlugin.Cleanup();

        // Act
        Should.Throw<InvalidOperationException>(() => TestPlugin.Instance)
              .Message.ShouldBe($"{typeof(TestPlugin)} was not created.");
    }

    public sealed class TestPlugin(IModdingContext moddingContext, IMod mod) : PluginBase<TestPlugin>(moddingContext, mod)
    {
        private static readonly FieldInfo _IsEnabled = typeof(PluginBase<TestPlugin>).GetField("_IsEnabled", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo _Instance  = typeof(PluginBase<TestPlugin>).GetField("_Instance", BindingFlags.Static | BindingFlags.NonPublic)!;

        public void SetIsEnabled(bool value) => _IsEnabled.SetValue(this, value);

        public static void Cleanup() => _Instance.SetValue(null!, null!);

        public readonly List<bool> IsEnabledChanges = new();

        protected override void OnIsEnabledChanged() {
            base.OnIsEnabledChanged();
            IsEnabledChanges.Add(IsEnabled);
        }
    }
}
