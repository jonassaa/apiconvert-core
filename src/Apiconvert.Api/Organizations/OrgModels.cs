namespace Apiconvert.Api.Organizations;

public sealed record OrgMembershipSummary
{
    public Guid OrgId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Role { get; init; } = "member";
}

public sealed record OrgSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public sealed record OrgListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Role { get; init; } = "member";
    public bool IsFavorite { get; init; }
}
