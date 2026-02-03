namespace Apiconvert.Api.Converters;

public sealed record ConverterSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public bool LogRequestsEnabled { get; init; }
    public string ForwardUrl { get; init; } = string.Empty;
}

public sealed record ConverterDetail
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string InboundPath { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string ForwardUrl { get; init; } = string.Empty;
    public string? ForwardMethod { get; init; }
    public Dictionary<string, string> ForwardHeaders { get; init; } = new();
    public bool LogRequestsEnabled { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthValueHash { get; init; }
    public string? InboundAuthValueLast4 { get; init; }
    public string? InboundSecretHash { get; init; }
    public string? InboundSecretLast4 { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }
}

public sealed record ConverterMappingSnapshot
{
    public string MappingJson { get; init; } = string.Empty;
    public string? InputSample { get; init; }
    public string? OutputSample { get; init; }
}

public sealed record ConverterDetailBundle
{
    public ConverterDetail Converter { get; init; } = new();
    public ConverterMappingSnapshot? Mapping { get; init; }
    public IReadOnlyList<Admin.ConverterLogSummary> Logs { get; init; } =
        Array.Empty<Admin.ConverterLogSummary>();
}
