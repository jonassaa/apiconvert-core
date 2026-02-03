namespace Apiconvert.Api.Inbound;

public sealed record ConverterConfig
{
    public Guid Id { get; init; }
    public Guid OrgId { get; init; }
    public bool Enabled { get; init; }
    public string ForwardUrl { get; init; } = string.Empty;
    public string? ForwardMethod { get; init; }
    public Dictionary<string, string> ForwardHeaders { get; init; } = new();
    public bool LogRequestsEnabled { get; init; }
    public string? InboundSecretHash { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthValueHash { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }
}

public sealed record InboundRequest
{
    public Guid OrgId { get; init; }
    public string InboundPath { get; init; } = string.Empty;
    public string Method { get; init; } = "GET";
    public string Url { get; init; } = string.Empty;
    public string QueryString { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Dictionary<string, string> Headers { get; init; } = new();
    public string? SourceIp { get; init; }
    public long? ContentLength { get; init; }
}

public sealed record InboundResponse
{
    public int StatusCode { get; init; }
    public object? JsonBody { get; init; }
    public string? TextBody { get; init; }
    public string? ContentType { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
}

public sealed record ForwardRequest
{
    public string Url { get; init; } = string.Empty;
    public string Method { get; init; } = "POST";
    public Dictionary<string, string> Headers { get; init; } = new();
    public string? Body { get; init; }
}

public sealed record ForwardResult
{
    public int StatusCode { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public object? JsonBody { get; init; }
    public string? TextBody { get; init; }
    public string? ContentType { get; init; }
    public string? Error { get; init; }
    public int DurationMs { get; init; }
}

public sealed record ConverterLogEntry
{
    public Guid ConverterId { get; init; }
    public Guid OrgId { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public Guid RequestId { get; init; }
    public string? SourceIp { get; init; }
    public string? Method { get; init; }
    public string? Path { get; init; }
    public object? HeadersJson { get; init; }
    public object? QueryJson { get; init; }
    public object? BodyJson { get; init; }
    public object? TransformedBodyJson { get; init; }
    public string? ForwardUrl { get; init; }
    public int? ForwardStatus { get; init; }
    public int? ForwardResponseMs { get; init; }
    public string? ErrorText { get; init; }
    public object? ForwardResponseHeadersJson { get; init; }
    public object? ForwardResponseBodyJson { get; init; }
    public string? ForwardResponseBodyText { get; init; }
}

public static class InboundConstants
{
    public static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "PATCH"];
    public const int DefaultTimeoutMs = 10_000;
    public const int MaxInboundBodyBytes = 1_000_000;
    public const int RateLimitWindowMs = 60_000;
    public const int RateLimitMaxRequests = 60;
    public const int DefaultLogBodyMaxBytes = 32_768;
    public const int DefaultLogHeadersMaxBytes = 8_192;
    public static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "cookie",
        "set-cookie",
        "x-apiconvert-token"
    };
}
