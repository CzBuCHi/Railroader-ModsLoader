using System;

namespace Railroader.ModInterfaces;

/// <summary> Specifies the comparison operator used for version constraints in a <see cref="FluentVersion"/>. </summary>
public enum VersionOperator
{
    Equal,
    GreaterThan,
    GreaterOrEqual,
    LessOrEqual,
    LessThan
}

/// <summary> Represents a version constraint for a mod, specifying an operator and a version. </summary>
/// <param name="Version">The version number to compare against.</param>
/// <param name="Operator">The operator defining the version constraint.</param>
public sealed record FluentVersion(Version Version, VersionOperator Operator = VersionOperator.Equal)
{
    public override string ToString() =>
        Operator switch {
            VersionOperator.Equal          => "",
            VersionOperator.GreaterThan    => ">",
            VersionOperator.GreaterOrEqual => ">=",
            VersionOperator.LessOrEqual    => "<=",
            VersionOperator.LessThan       => "<",
            _                              => $"[Invalid operator: {Operator}]"
        } + Version;
}
