namespace Apiconvert.Api.Converters;

public sealed record ConverterCreateRequest
{
    public Guid OrgId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string InboundPath { get; init; } = string.Empty;
    public string ForwardUrl { get; init; } = string.Empty;
    public string? ForwardMethod { get; init; }
    public string? ForwardHeadersJson { get; init; }
    public bool? Enabled { get; init; }
    public bool? LogRequestsEnabled { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthToken { get; init; }
    public string? InboundSecret { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthPassword { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthHeaderValue { get; init; }
    public string? OutboundAuthMode { get; init; }
    public string? OutboundAuthToken { get; init; }
    public string? OutboundAuthUsername { get; init; }
    public string? OutboundAuthPassword { get; init; }
    public string? OutboundCustomHeaderName { get; init; }
    public string? OutboundCustomHeaderValue { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogBodyMaxKb { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public int? LogHeadersMaxKb { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }
}

public sealed record ConverterUpdateRequest
{
    public string? Name { get; init; }
    public string? InboundPath { get; init; }
    public string? ForwardUrl { get; init; }
    public string? ForwardMethod { get; init; }
    public string? ForwardHeadersJson { get; init; }
    public bool? Enabled { get; init; }
    public bool? LogRequestsEnabled { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthToken { get; init; }
    public string? InboundSecret { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthPassword { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthHeaderValue { get; init; }
    public string? OutboundAuthMode { get; init; }
    public string? OutboundAuthToken { get; init; }
    public string? OutboundAuthUsername { get; init; }
    public string? OutboundAuthPassword { get; init; }
    public string? OutboundCustomHeaderName { get; init; }
    public string? OutboundCustomHeaderValue { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogBodyMaxKb { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public int? LogHeadersMaxKb { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }
}
