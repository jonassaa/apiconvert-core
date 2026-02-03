using Apiconvert.Api.Admin;
using Apiconvert.Api.Organizations;

namespace Apiconvert.Api.Dashboard;

public sealed class DashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IOrgMembershipRepository _membershipRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IOrgMembershipRepository membershipRepository)
    {
        _dashboardRepository = dashboardRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<ServiceResult<OrgDashboard>> GetDashboardAsync(
        Guid userId,
        Guid orgId,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role))
        {
            return ServiceResult<OrgDashboard>.Fail("Not a member of this organization", "forbidden");
        }

        var metrics = await _dashboardRepository.GetMetricsAsync(orgId, cancellationToken);
        var converters = await _dashboardRepository.GetRecentConvertersAsync(orgId, 5, cancellationToken);
        var members = await _dashboardRepository.GetRecentMembersAsync(orgId, 5, cancellationToken);
        var invites = await _dashboardRepository.GetRecentInvitesAsync(orgId, 5, cancellationToken);
        var logs = await _dashboardRepository.GetRecentLogsAsync(orgId, 10, cancellationToken);

        var dashboard = new OrgDashboard
        {
            Metrics = metrics,
            RecentConverters = converters,
            RecentMembers = members,
            RecentInvites = invites,
            RecentLogs = logs
        };

        return ServiceResult<OrgDashboard>.Success(dashboard);
    }
}
