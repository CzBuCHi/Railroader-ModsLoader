using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using _Assembly = System.Reflection.Assembly;

namespace Railroader.ModManagerInstaller.Abstractions;

public interface IAssembly
{
    _Assembly Assembly { get; }
    AssemblyName GetName();
    Stream? GetManifestResourceStream(string name);
    string Location { get; }
}

public interface IAssemblyStatic
{
    IAssembly GetExecutingAssembly();
    IAssembly Load(byte[] buffer);
}

[ExcludeFromCodeCoverage]
public sealed class AssemblyWrapper(_Assembly assembly) : IAssembly
{
    public _Assembly Assembly { get; } = assembly;

    public AssemblyName GetName() => Assembly.GetName();

    public Stream? GetManifestResourceStream(string name) => Assembly.GetManifestResourceStream(name);

    public string Location => Assembly.Location;
}

[ExcludeFromCodeCoverage]
public sealed class AssemblyStatic : IAssemblyStatic
{
    private static IAssembly CreateWrapper(_Assembly assembly) => new AssemblyWrapper(assembly);

    public IAssembly GetExecutingAssembly() => CreateWrapper(_Assembly.GetExecutingAssembly());

    public IAssembly Load(byte[] buffer) => CreateWrapper(_Assembly.Load(buffer));
}
