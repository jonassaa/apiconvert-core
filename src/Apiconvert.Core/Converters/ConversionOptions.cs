using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

/// <summary>
/// Runtime options that control conversion execution behavior.
/// </summary>
public sealed record ConversionOptions
{
    /// <summary>
    /// Policy used when multiple rules target the same output path.
    /// </summary>
    public OutputCollisionPolicy CollisionPolicy { get; init; } = OutputCollisionPolicy.LastWriteWins;
}
