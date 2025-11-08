using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

namespace Railroader.ModManagerInstaller.Abstractions;

public interface IRegistryStatic
{
    IRegistryKey CurrentUser { get; }
}

public interface IRegistryKey : IDisposable
{
    IRegistryKey? OpenSubKey(string name);
    object? GetValue(string name);
}

[ExcludeFromCodeCoverage]
public sealed class RegistryStatic : IRegistryStatic
{
    private static IRegistryKey CreateWrapper(RegistryKey registryKey) => new RegistryKeyWrapper(registryKey);

    public IRegistryKey CurrentUser => CreateWrapper(Registry.CurrentUser);
}

[ExcludeFromCodeCoverage]
public sealed class RegistryKeyWrapper(RegistryKey registryKey) : IRegistryKey
{
    private static IRegistryKey? CreateWrapper(RegistryKey? registryKey) =>
        registryKey != null ? new RegistryKeyWrapper(registryKey) : null;

    public IRegistryKey? OpenSubKey(string name) => CreateWrapper(registryKey.OpenSubKey(name));

    public object? GetValue(string name) => registryKey.GetValue(name);

    public void Dispose() => registryKey.Dispose();
}
