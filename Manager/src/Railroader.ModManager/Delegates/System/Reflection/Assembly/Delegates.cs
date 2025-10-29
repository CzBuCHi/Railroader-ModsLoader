using _Assembly = System.Reflection.Assembly;

namespace Railroader.ModManager.Delegates.System.Reflection.Assembly;

/// <inheritdoc cref="_Assembly.LoadFrom(string)"/>
/// <remarks> Wraps <see cref="_Assembly.LoadFrom(string)"/> for testability. </remarks>
internal delegate _Assembly? LoadFrom(string assemblyFile);