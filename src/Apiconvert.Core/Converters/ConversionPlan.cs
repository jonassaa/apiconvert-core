using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

/// <summary>
/// Compiled conversion plan that reuses normalized rules across conversions.
/// </summary>
public sealed class ConversionPlan
{
    internal ConversionPlan(ConversionRules rules)
    {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        CacheKey = RulesCacheKey.Compute(rules);
    }

    /// <summary>
    /// Normalized rules used by this plan.
    /// </summary>
    public ConversionRules Rules { get; }

    /// <summary>
    /// Stable hash key for the normalized rules artifact.
    /// </summary>
    public string CacheKey { get; }

    /// <summary>
    /// Applies this plan to the provided input payload.
    /// </summary>
    /// <param name="input">Input payload (already parsed).</param>
    /// <param name="options">Optional execution options.</param>
    /// <returns>Conversion result containing output and diagnostics.</returns>
    public ConversionResult Apply(object? input, ConversionOptions? options = null)
    {
        return MappingExecutor.ApplyConversion(input, Rules, options);
    }
}
