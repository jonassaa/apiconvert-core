using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Contracts;

/// <summary>
/// Request model for generating conversion rules from example payloads.
/// </summary>
public sealed record ConversionRulesGenerationRequest
{
    /// <summary>
    /// Input payload format.
    /// </summary>
    public DataFormat InputFormat { get; init; } = DataFormat.Json;

    /// <summary>
    /// Output payload format.
    /// </summary>
    public DataFormat OutputFormat { get; init; } = DataFormat.Json;

    /// <summary>
    /// Example input payload used for generation.
    /// </summary>
    public string InputSample { get; init; } = string.Empty;

    /// <summary>
    /// Example output payload used for generation.
    /// </summary>
    public string OutputSample { get; init; } = string.Empty;

    /// <summary>
    /// Optional model identifier for the generator implementation.
    /// </summary>
    public string? Model { get; init; }
}

/// <summary>
/// Generates conversion rules from example payloads.
/// </summary>
public interface IConversionRulesGenerator
{
    /// <summary>
    /// Generates conversion rules for the given request.
    /// </summary>
    /// <param name="request">Generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated conversion rules.</returns>
    Task<ConversionRules> GenerateAsync(ConversionRulesGenerationRequest request, CancellationToken cancellationToken);
}
