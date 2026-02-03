namespace Apiconvert.Infrastructure.Ai;

public sealed record OpenRouterOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1";
    public string Model { get; init; } = "openai/gpt-4o-mini";
    public string? SiteUrl { get; init; }
    public string? AppName { get; init; }
}
