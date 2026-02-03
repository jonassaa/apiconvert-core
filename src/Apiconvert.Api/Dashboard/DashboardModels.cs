namespace Apiconvert.Api.Dashboard;

public sealed record DashboardMetrics
{
    public long Requests24h { get; init; }
    public long Requests7d { get; init; }
    public long Success7d { get; init; }
    public decimal AvgResponseMs { get; init; }
}

public sealed record DashboardRecentConverter
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record DashboardRecentMember
{
    public Guid Id { get; init; }
    public string Role { get; init; } = "member";
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record DashboardRecentInvite
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
}

public sealed record DashboardRecentLog
{
    public DateTimeOffset ReceivedAt { get; init; }
    public Guid RequestId { get; init; }
    public int? ForwardStatus { get; init; }
    public int? ForwardResponseMs { get; init; }
    public string? ConverterName { get; init; }
}

public sealed record OrgDashboard
{
    public DashboardMetrics Metrics { get; init; } = new();
    public IReadOnlyList<DashboardRecentConverter> RecentConverters { get; init; } = Array.Empty<DashboardRecentConverter>();
    public IReadOnlyList<DashboardRecentMember> RecentMembers { get; init; } = Array.Empty<DashboardRecentMember>();
    public IReadOnlyList<DashboardRecentInvite> RecentInvites { get; init; } = Array.Empty<DashboardRecentInvite>();
    public IReadOnlyList<DashboardRecentLog> RecentLogs { get; init; } = Array.Empty<DashboardRecentLog>();
}
