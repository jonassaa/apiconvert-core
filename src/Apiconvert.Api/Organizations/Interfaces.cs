using Apiconvert.Api.Dashboard;

namespace Apiconvert.Api.Organizations;

public interface IOrgRepository
{
    Task<IReadOnlyList<OrgMembershipSummary>> GetMembershipsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetFavoriteOrgIdsAsync(Guid userId, CancellationToken cancellationToken);
    Task<OrgSummary> CreateOrgAsync(Guid userId, string name, CancellationToken cancellationToken);
    Task AddFavoriteAsync(Guid userId, Guid orgId, CancellationToken cancellationToken);
    Task RemoveFavoriteAsync(Guid userId, Guid orgId, CancellationToken cancellationToken);
}

public interface IOrgSettingsRepository
{
    Task<OrgSummary?> GetOrgAsync(Guid orgId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrgMember>> GetMembersAsync(Guid orgId, CancellationToken cancellationToken);
    Task<OrgMember?> GetMemberAsync(Guid orgId, Guid memberId, CancellationToken cancellationToken);
    Task<int> GetOwnerCountAsync(Guid orgId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrgInvite>> GetInvitesAsync(Guid orgId, CancellationToken cancellationToken);
    Task<OrgInvite> CreateInviteAsync(Guid orgId, Guid createdBy, string email, string role, string token, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<bool> UpdateOrgNameAsync(Guid orgId, string name, CancellationToken cancellationToken);
    Task<bool> DeleteOrgAsync(Guid orgId, CancellationToken cancellationToken);
    Task<bool> UpdateMemberRoleAsync(Guid orgId, Guid memberId, string role, CancellationToken cancellationToken);
    Task<bool> RemoveMemberAsync(Guid orgId, Guid memberId, CancellationToken cancellationToken);
}

public interface IUserDirectory
{
    Task<IReadOnlyList<UserProfile>> GetProfilesAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken);
}

public interface IInviteRepository
{
    Task<InviteDetails?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<bool> AcceptInviteAsync(Guid inviteId, Guid orgId, Guid userId, string role, DateTimeOffset acceptedAt, CancellationToken cancellationToken);
}

public interface IDashboardRepository
{
    Task<DashboardMetrics> GetMetricsAsync(Guid orgId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardRecentConverter>> GetRecentConvertersAsync(Guid orgId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardRecentMember>> GetRecentMembersAsync(Guid orgId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardRecentInvite>> GetRecentInvitesAsync(Guid orgId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardRecentLog>> GetRecentLogsAsync(Guid orgId, int limit, CancellationToken cancellationToken);
}
