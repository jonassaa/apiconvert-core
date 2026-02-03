using System.Security.Claims;
using Apiconvert.Api.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/invites")]
public sealed class InvitesController : ControllerBase
{
    private readonly InviteService _service;

    public InvitesController(InviteService service)
    {
        _service = service;
    }

    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInvite([FromRoute] string token, CancellationToken cancellationToken)
    {
        var result = await _service.GetInviteAsync(token, cancellationToken);
        if (!result.Ok || result.Value == null)
        {
            return NotFound(new { error = result.Error ?? "Invite not found" });
        }

        var invite = result.Value;
        return Ok(new
        {
            invite = new
            {
                invite.Id,
                invite.OrgId,
                invite.OrgName,
                invite.Email,
                invite.Role,
                invite.ExpiresAt,
                invite.AcceptedAt
            }
        });
    }

    [HttpPost("{token}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptInvite([FromRoute] string token, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var email = ResolveUserEmail();
        var result = await _service.AcceptInviteAsync(userId.Value, email, token, cancellationToken);
        if (!result.Ok || result.Value == null)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to accept invite");
        }

        return Ok(new { orgId = result.Value.OrgId, alreadyAccepted = result.Value.AlreadyAccepted });
    }

    private Guid? ResolveUserId()
    {
        var header = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(header, out var userId))
        {
            return userId;
        }

        return null;
    }

    private string? ResolveUserEmail()
    {
        return User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private IActionResult MapError(string? code, string message)
    {
        return code switch
        {
            "not_found" => NotFound(new { error = message }),
            "expired" => StatusCode(410, new { error = message }),
            "email_mismatch" => StatusCode(403, new { error = message }),
            _ => BadRequest(new { error = message })
        };
    }
}
