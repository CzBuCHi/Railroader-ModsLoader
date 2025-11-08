using System;
using System.Diagnostics.CodeAnalysis;

namespace Railroader.ModManagerInstaller;

[ExcludeFromCodeCoverage]
public class InstallerException : Exception
{
    public InstallerException(string message)
        : base(message) {
    }

    public InstallerException(string message, Exception innerException)
        : base(message, innerException) {
    }
}