using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Railroader.ModInjector.Wrappers;

/// <summary> Wrapper for <see cref="Assembly"/>. </summary>
internal interface IAssemblyWrapper
{
    /// <inheritdoc cref="Assembly.LoadFrom(string)"/>
    Assembly LoadFrom(string assemblyFile);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class AssemblyWrapper : IAssemblyWrapper
{
    /// <inheritdoc />
    public Assembly LoadFrom(string assemblyFile) => Assembly.LoadFrom(assemblyFile);
}
