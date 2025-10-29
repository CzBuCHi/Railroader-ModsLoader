using System.Collections.Generic;
using _Directory = System.IO.Directory;

namespace Railroader.ModManager.Delegates.System.IO.Directory;

/// <inheritdoc cref="_Directory.EnumerateDirectories(string)"/>
/// <remarks> Wraps <see cref="_Directory.EnumerateDirectories(string)"/> for testability. </remarks>
internal delegate IEnumerable<string> EnumerateDirectories(string path);

/// <inheritdoc cref="_Directory.GetCurrentDirectory()"/>
/// <remarks> Wraps <see cref="_Directory.GetCurrentDirectory()"/> for testability. </remarks>
internal delegate string GetCurrentDirectory();
