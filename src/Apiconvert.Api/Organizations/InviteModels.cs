namespace Apiconvert.Api.Organizations;

public sealed record InviteDetails
{
    public Guid Id { get; init; }
    public Guid OrgId { get; init; }
    public string OrgName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "member";
    public string Token { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
}

public sealed record InviteAcceptance
{
    public Guid OrgId { get; init; }
    public bool AlreadyAccepted { get; init; }
}
