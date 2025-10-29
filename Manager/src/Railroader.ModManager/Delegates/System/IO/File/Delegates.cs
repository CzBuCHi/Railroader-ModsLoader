using System;
using System.IO;
using _File = System.IO.File;

namespace Railroader.ModManager.Delegates.System.IO.File;

/// <inheritdoc cref="_File.Exists(string)"/>
/// <remarks> Wraps <see cref="_File.Exists(string)"/> for testability. </remarks>
internal delegate bool Exists(string path);

/// <inheritdoc cref="_File.ReadAllText(string)"/>
/// <remarks> Wraps <see cref="_File.ReadAllText(string)"/> for testability. </remarks>
internal delegate string ReadAllText(string path);

/// <inheritdoc cref="_File.GetLastWriteTime(string)"/>
/// <remarks> Wraps <see cref="_File.GetLastWriteTime(string)"/> for testability. </remarks>
internal delegate DateTime GetLastWriteTime(string path);

/// <inheritdoc cref="_File.Delete(string)"/>
/// <remarks> Wraps <see cref="_File.Delete(string)"/> for testability. </remarks>
internal delegate void Delete(string path);

/// <inheritdoc cref="_File.Move(string, string)"/>
/// <remarks> Wraps <see cref="_File.Move(string, string)"/> for testability. </remarks>
internal delegate void Move(string sourceFileName, string destFileName);

/// <inheritdoc cref="_File.Create(string)"/>
/// <remarks> Wraps <see cref="_File.Create(string)"/> for testability. </remarks>
internal delegate Stream Create(string path);
