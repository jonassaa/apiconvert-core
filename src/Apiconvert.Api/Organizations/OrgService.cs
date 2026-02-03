using Apiconvert.Api.Admin;

namespace Apiconvert.Api.Organizations;

public sealed class OrgService
{
    private readonly IOrgRepository _orgRepository;
    private readonly IOrgMembershipRepository _membershipRepository;

    public OrgService(IOrgRepository orgRepository, IOrgMembershipRepository membershipRepository)
    {
        _orgRepository = orgRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<OrgListItem>>> GetOrgListAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var memberships = await _orgRepository.GetMembershipsAsync(userId, cancellationToken);
        var favorites = await _orgRepository.GetFavoriteOrgIdsAsync(userId, cancellationToken);

        var items = memberships
            .Select(member => new OrgListItem
            {
                Id = member.OrgId,
                Name = member.Name,
                Slug = member.Slug,
                Role = member.Role,
                IsFavorite = favorites.Contains(member.OrgId)
            })
            .ToList();

        return ServiceResult<IReadOnlyList<OrgListItem>>.Success(items);
    }

    public async Task<ServiceResult<OrgSummary>> CreateOrgAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<OrgSummary>.Fail("Organization name is required.");
        }

        var org = await _orgRepository.CreateOrgAsync(userId, name.Trim(), cancellationToken);
        return ServiceResult<OrgSummary>.Success(org);
    }

    public async Task<ServiceResult<bool>> SetFavoriteAsync(
        Guid userId,
        Guid orgId,
        bool isFavorite,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role))
        {
            return ServiceResult<bool>.Fail("Not a member of this organization", "forbidden");
        }

        if (isFavorite)
        {
            await _orgRepository.AddFavoriteAsync(userId, orgId, cancellationToken);
        }
        else
        {
            await _orgRepository.RemoveFavoriteAsync(userId, orgId, cancellationToken);
        }

        return ServiceResult<bool>.Success(true);
    }
}
