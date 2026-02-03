using System.Security.Cryptography;
using Apiconvert.Api.Admin;

namespace Apiconvert.Api.Organizations;

public sealed class OrgSettingsService
{
    private readonly IOrgSettingsRepository _settingsRepository;
    private readonly IOrgMembershipRepository _membershipRepository;
    private readonly IUserDirectory _userDirectory;

    public OrgSettingsService(
        IOrgSettingsRepository settingsRepository,
        IOrgMembershipRepository membershipRepository,
        IUserDirectory userDirectory)
    {
        _settingsRepository = settingsRepository;
        _membershipRepository = membershipRepository;
        _userDirectory = userDirectory;
    }

    public async Task<ServiceResult<OrgSettings>> GetSettingsAsync(
        Guid userId,
        Guid orgId,
        CancellationToken cancellationToken)
    {
        var org = await _settingsRepository.GetOrgAsync(orgId, cancellationToken);
        if (org == null)
        {
            return ServiceResult<OrgSettings>.Fail("Organization not found", "org_not_found");
        }

        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role))
        {
            return ServiceResult<OrgSettings>.Fail("Not a member of this organization", "forbidden");
        }

        var members = await _settingsRepository.GetMembersAsync(orgId, cancellationToken);
        var userIds = members.Select(member => member.UserId).ToList();
        var profiles = await _userDirectory.GetProfilesAsync(userIds, cancellationToken);
        var profileMap = profiles.ToDictionary(profile => profile.UserId, profile => profile);

        var membersView = members.Select(member =>
        {
            profileMap.TryGetValue(member.UserId, out var profile);
            return new OrgMemberView
            {
                UserId = member.UserId,
                Role = member.Role,
                Name = profile?.Name,
                Email = profile?.Email
            };
        }).ToList();

        var invites = await _settingsRepository.GetInvitesAsync(orgId, cancellationToken);
        var settings = new OrgSettings
        {
            Org = org,
            UserRole = role,
            CanManage = IsAdminRole(role),
            IsOwner = role == "owner",
            Members = membersView,
            Invites = invites
        };

        return ServiceResult<OrgSettings>.Success(settings);
    }

    public async Task<ServiceResult<OrgInvite>> CreateInviteAsync(
        Guid userId,
        Guid orgId,
        string email,
        string role,
        CancellationToken cancellationToken)
    {
        var userRole = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(userRole) || !IsAdminRole(userRole))
        {
            return ServiceResult<OrgInvite>.Fail("Only admins can invite members", "forbidden");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return ServiceResult<OrgInvite>.Fail("Invite email is required.");
        }

        if (!IsValidRole(role))
        {
            return ServiceResult<OrgInvite>.Fail("Invalid role.");
        }

        var token = GenerateToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var invite = await _settingsRepository.CreateInviteAsync(
            orgId,
            userId,
            email.Trim(),
            role,
            token,
            expiresAt,
            cancellationToken);

        return ServiceResult<OrgInvite>.Success(invite);
    }

    public async Task<ServiceResult<bool>> UpdateOrgNameAsync(
        Guid userId,
        Guid orgId,
        string name,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role) || !IsAdminRole(role))
        {
            return ServiceResult<bool>.Fail("Only admins can update organizations", "forbidden");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<bool>.Fail("Name is required.");
        }

        var updated = await _settingsRepository.UpdateOrgNameAsync(orgId, name.Trim(), cancellationToken);
        return updated
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Organization not found", "org_not_found");
    }

    public async Task<ServiceResult<bool>> DeleteOrgAsync(
        Guid userId,
        Guid orgId,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (role != "owner")
        {
            return ServiceResult<bool>.Fail("Only owners can delete organizations", "forbidden");
        }

        var deleted = await _settingsRepository.DeleteOrgAsync(orgId, cancellationToken);
        return deleted
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Organization not found", "org_not_found");
    }

    public async Task<ServiceResult<bool>> UpdateMemberRoleAsync(
        Guid userId,
        Guid orgId,
        Guid memberId,
        string nextRole,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role) || !IsAdminRole(role))
        {
            return ServiceResult<bool>.Fail("Only admins can manage members", "forbidden");
        }

        if (!IsValidRole(nextRole))
        {
            return ServiceResult<bool>.Fail("Invalid role.");
        }

        var member = await _settingsRepository.GetMemberAsync(orgId, memberId, cancellationToken);
        if (member == null)
        {
            return ServiceResult<bool>.Fail("Member not found", "not_found");
        }

        var ownerCount = await _settingsRepository.GetOwnerCountAsync(orgId, cancellationToken);
        if (!CanChangeMemberRole(member.Role, nextRole, ownerCount))
        {
            return ServiceResult<bool>.Fail("Cannot demote last owner");
        }

        var updated = await _settingsRepository.UpdateMemberRoleAsync(orgId, memberId, nextRole, cancellationToken);
        return updated
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Member not found", "not_found");
    }

    public async Task<ServiceResult<bool>> RemoveMemberAsync(
        Guid userId,
        Guid orgId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role) || !IsAdminRole(role))
        {
            return ServiceResult<bool>.Fail("Only admins can manage members", "forbidden");
        }

        var member = await _settingsRepository.GetMemberAsync(orgId, memberId, cancellationToken);
        if (member == null)
        {
            return ServiceResult<bool>.Fail("Member not found", "not_found");
        }

        var ownerCount = await _settingsRepository.GetOwnerCountAsync(orgId, cancellationToken);
        if (!CanRemoveMember(member.Role, ownerCount))
        {
            return ServiceResult<bool>.Fail("Cannot remove last owner");
        }

        var removed = await _settingsRepository.RemoveMemberAsync(orgId, memberId, cancellationToken);
        return removed
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Member not found", "not_found");
    }

    private static bool IsAdminRole(string? role)
    {
        return role == "owner" || role == "admin";
    }

    private static bool IsValidRole(string? role)
    {
        return role == "owner" || role == "admin" || role == "member";
    }

    private static bool CanChangeMemberRole(string currentRole, string nextRole, int ownerCount)
    {
        return !(currentRole == "owner" && nextRole != "owner" && ownerCount <= 1);
    }

    private static bool CanRemoveMember(string currentRole, int ownerCount)
    {
        return !(currentRole == "owner" && ownerCount <= 1);
    }

    private static string GenerateToken()
    {
        var buffer = RandomNumberGenerator.GetBytes(20);
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }
}
