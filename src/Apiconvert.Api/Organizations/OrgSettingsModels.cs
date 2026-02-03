namespace Apiconvert.Api.Organizations;

public sealed record OrgMember
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = "member";
}

public sealed record OrgInvite
{
    public Guid Id { get; init; }
    public Guid OrgId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "member";
    public string Token { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record UserProfile
{
    public Guid UserId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
}

public sealed record OrgMemberView
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = "member";
    public string? Name { get; init; }
    public string? Email { get; init; }
}

public sealed record OrgSettings
{
    public OrgSummary Org { get; init; } = new();
    public string UserRole { get; init; } = "member";
    public bool CanManage { get; init; }
    public bool IsOwner { get; init; }
    public IReadOnlyList<OrgMemberView> Members { get; init; } = Array.Empty<OrgMemberView>();
    public IReadOnlyList<OrgInvite> Invites { get; init; } = Array.Empty<OrgInvite>();
}
