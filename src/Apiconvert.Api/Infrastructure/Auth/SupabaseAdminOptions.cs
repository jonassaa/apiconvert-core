namespace Apiconvert.Infrastructure.Auth;

public sealed record SupabaseAdminOptions
{
    public string Url { get; init; } = string.Empty;
    public string ServiceRoleKey { get; init; } = string.Empty;
}
