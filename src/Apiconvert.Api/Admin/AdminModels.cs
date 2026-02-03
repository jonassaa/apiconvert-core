namespace Apiconvert.Api.Admin;

public sealed record AdminConverterCreateRequest
{
    public Guid OrgId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string InboundPath { get; init; } = string.Empty;
    public string ForwardUrl { get; init; } = string.Empty;
    public string? ForwardMethod { get; init; }
    public Dictionary<string, string> ForwardHeaders { get; init; } = new();
    public bool Enabled { get; init; }
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

public sealed record AdminMappingSaveRequest
{
    public Guid OrgId { get; init; }
    public Guid ConverterId { get; init; }
    public string MappingJson { get; init; } = string.Empty;
    public string? InputSample { get; init; }
    public string? OutputSample { get; init; }
}

public sealed record ConverterLogSummary
{
    public DateTimeOffset ReceivedAt { get; init; }
    public int? ForwardStatus { get; init; }
    public int? ForwardResponseMs { get; init; }
    public Guid RequestId { get; init; }
}

public sealed record ServiceResult<T>(bool Ok, T? Value, string? Error, string? ErrorCode)
{
    public static ServiceResult<T> Success(T value) => new(true, value, null, null);
    public static ServiceResult<T> Fail(string error, string? code = null) => new(false, default, error, code);
}
