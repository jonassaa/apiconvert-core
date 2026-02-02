using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Contracts;

public sealed record ConversionRulesGenerationRequest
{
    public DataFormat InputFormat { get; init; } = DataFormat.Json;
    public DataFormat OutputFormat { get; init; } = DataFormat.Json;
    public string InputSample { get; init; } = string.Empty;
    public string OutputSample { get; init; } = string.Empty;
    public string? Model { get; init; }
}

public interface IConversionRulesGenerator
{
    Task<ConversionRules> GenerateAsync(ConversionRulesGenerationRequest request, CancellationToken cancellationToken);
}
