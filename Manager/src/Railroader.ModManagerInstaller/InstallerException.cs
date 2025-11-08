using System;
using System.Diagnostics.CodeAnalysis;

namespace Railroader.ModManagerInstaller;

[ExcludeFromCodeCoverage]
public class InstallerException(string message) : Exception(message);