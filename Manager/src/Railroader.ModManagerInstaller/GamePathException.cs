using System;
using System.Diagnostics.CodeAnalysis;

namespace Railroader.ModManagerInstaller;

[ExcludeFromCodeCoverage]
public class GamePathException(string message) : Exception(message);