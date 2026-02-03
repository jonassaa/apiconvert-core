using Apiconvert.Api.Admin;

namespace Apiconvert.Api.Organizations;

public sealed class InviteService
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IUserDirectory _userDirectory;

    public InviteService(IInviteRepository inviteRepository, IUserDirectory userDirectory)
    {
        _inviteRepository = inviteRepository;
        _userDirectory = userDirectory;
    }

    public async Task<ServiceResult<InviteDetails>> GetInviteAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult<InviteDetails>.Fail("Invite token is required.");
        }

        var invite = await _inviteRepository.GetByTokenAsync(token.Trim(), cancellationToken);
        if (invite == null)
        {
            return ServiceResult<InviteDetails>.Fail("Invite not found", "not_found");
        }

        return ServiceResult<InviteDetails>.Success(invite);
    }

    public async Task<ServiceResult<InviteAcceptance>> AcceptInviteAsync(
        Guid userId,
        string? userEmail,
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult<InviteAcceptance>.Fail("Invite token is required.");
        }

        var invite = await _inviteRepository.GetByTokenAsync(token.Trim(), cancellationToken);
        if (invite == null)
        {
            return ServiceResult<InviteAcceptance>.Fail("Invite not found", "not_found");
        }

        if (invite.AcceptedAt.HasValue)
        {
            return ServiceResult<InviteAcceptance>.Success(new InviteAcceptance
            {
                OrgId = invite.OrgId,
                AlreadyAccepted = true
            });
        }

        if (invite.ExpiresAt.UtcDateTime < DateTimeOffset.UtcNow)
        {
            return ServiceResult<InviteAcceptance>.Fail("Invite expired", "expired");
        }

        var resolvedEmail = userEmail;
        if (string.IsNullOrWhiteSpace(resolvedEmail))
        {
            var profile = await _userDirectory.GetProfilesAsync(new[] { userId }, cancellationToken);
            resolvedEmail = profile.FirstOrDefault()?.Email;
        }

        if (string.IsNullOrWhiteSpace(resolvedEmail) ||
            !string.Equals(resolvedEmail.Trim(), invite.Email, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<InviteAcceptance>.Fail("Invite email mismatch", "email_mismatch");
        }

        var acceptedAt = DateTimeOffset.UtcNow;
        var updated = await _inviteRepository.AcceptInviteAsync(
            invite.Id,
            invite.OrgId,
            userId,
            invite.Role,
            acceptedAt,
            cancellationToken);

        if (!updated)
        {
            return ServiceResult<InviteAcceptance>.Fail("Failed to accept invite");
        }

        return ServiceResult<InviteAcceptance>.Success(new InviteAcceptance
        {
            OrgId = invite.OrgId,
            AlreadyAccepted = false
        });
    }
}
