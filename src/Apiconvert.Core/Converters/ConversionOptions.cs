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

    /// <summary>
    /// Enables per-rule execution tracing in <see cref="ConversionResult.Trace"/>.
    /// </summary>
    public bool Explain { get; init; }

    /// <summary>
    /// Optional custom transform registry used by <c>source.type=transform</c> rules with <c>customTransform</c>.
    /// </summary>
    public IReadOnlyDictionary<string, Func<object?, object?>> TransformRegistry { get; init; }
        = new Dictionary<string, Func<object?, object?>>(StringComparer.Ordinal);
}
