using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Railroader.ModInjector.Wrappers;

// wrappers around Assembly.LoadFrom method to simplify testing

public interface IAssemblyLoader
{
    Assembly Load(string assemblyFile);
}

[ExcludeFromCodeCoverage]
public sealed class AssemblyLoader : IAssemblyLoader
{
    public Assembly Load(string assemblyFile) => Assembly.LoadFrom(assemblyFile);
}
