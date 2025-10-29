using System.Reflection;

namespace Railroader.ModManager.Delegates;

/// <inheritdoc cref="Assembly.LoadFrom(string)"/>
internal delegate Assembly? LoadAssemblyFromDelegate(string assemblyFile);