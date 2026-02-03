using Apiconvert.Api.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/orgs/{orgId:guid}/converters")]
[Authorize]
public sealed class ConvertersController : ControllerBase
{
    private readonly ConverterAdminService _service;

    public ConvertersController(ConverterAdminService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromRoute] Guid orgId,
        [FromBody] ConverterCreateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var request = new AdminConverterCreateRequest
        {
            OrgId = orgId,
            Name = dto.Name,
            InboundPath = dto.InboundPath,
            ForwardUrl = dto.ForwardUrl,
            ForwardMethod = dto.ForwardMethod,
            ForwardHeaders = dto.ForwardHeaders ?? new Dictionary<string, string>(),
            Enabled = dto.Enabled,
            LogRequestsEnabled = dto.LogRequestsEnabled,
            InboundAuthMode = dto.InboundAuthMode,
            InboundAuthHeaderName = dto.InboundAuthHeaderName,
            InboundAuthUsername = dto.InboundAuthUsername,
            InboundAuthValueHash = dto.InboundAuthValueHash,
            InboundAuthValueLast4 = dto.InboundAuthValueLast4,
            InboundSecretHash = dto.InboundSecretHash,
            InboundSecretLast4 = dto.InboundSecretLast4,
            LogRetentionDays = dto.LogRetentionDays,
            LogBodyMaxBytes = dto.LogBodyMaxBytes,
            LogHeadersMaxBytes = dto.LogHeadersMaxBytes,
            LogRedactSensitiveHeaders = dto.LogRedactSensitiveHeaders,
            InboundResponseMode = dto.InboundResponseMode
        };

        var result = await _service.CreateConverterAsync(userId.Value, request, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to create converter");
        }

        return Ok(new { id = result.Value });
    }

    [HttpPatch("{converterId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid orgId,
        [FromRoute] Guid converterId,
        [FromBody] ConverterUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var updates = dto.ToUpdates();
        var result = await _service.UpdateConverterAsync(userId.Value, orgId, converterId, updates, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to update converter");
        }

        return Ok(new { ok = true });
    }

    [HttpDelete("{converterId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid orgId,
        [FromRoute] Guid converterId,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.DeleteConverterAsync(userId.Value, orgId, converterId, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to delete converter");
        }

        return Ok(new { ok = true });
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

    private IActionResult MapError(string? code, string message)
    {
        return code switch
        {
            "forbidden" => StatusCode(403, new { error = message }),
            "not_found" => NotFound(new { error = message }),
            "org_not_found" => NotFound(new { error = message }),
            _ => BadRequest(new { error = message })
        };
    }
}

public sealed record ConverterCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string InboundPath { get; init; } = string.Empty;
    public string ForwardUrl { get; init; } = string.Empty;
    public string? ForwardMethod { get; init; }
    public Dictionary<string, string>? ForwardHeaders { get; init; }
    public bool Enabled { get; init; }
    public bool LogRequestsEnabled { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthValueHash { get; init; }
    public string? InboundAuthValueLast4 { get; init; }
    public string? InboundSecretHash { get; init; }
    public string? InboundSecretLast4 { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }
}

public sealed record ConverterUpdateDto
{
    public string? Name { get; init; }
    public string? InboundPath { get; init; }
    public string? ForwardUrl { get; init; }
    public string? ForwardMethod { get; init; }
    public Dictionary<string, string>? ForwardHeaders { get; init; }
    public bool? Enabled { get; init; }
    public bool? LogRequestsEnabled { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthValueHash { get; init; }
    public string? InboundAuthValueLast4 { get; init; }
    public string? InboundSecretHash { get; init; }
    public string? InboundSecretLast4 { get; init; }

    public Dictionary<string, object?> ToUpdates()
    {
        var updates = new Dictionary<string, object?>();
        if (Name != null) updates["name"] = Name;
        if (InboundPath != null) updates["inbound_path"] = InboundPath;
        if (ForwardUrl != null) updates["forward_url"] = ForwardUrl;
        if (ForwardMethod != null) updates["forward_method"] = ForwardMethod;
        if (ForwardHeaders != null) updates["forward_headers_json"] = ForwardHeaders;
        if (Enabled != null) updates["enabled"] = Enabled;
        if (LogRequestsEnabled != null) updates["log_requests_enabled"] = LogRequestsEnabled;
        if (LogRetentionDays != null) updates["log_retention_days"] = LogRetentionDays;
        if (LogBodyMaxBytes != null) updates["log_body_max_bytes"] = LogBodyMaxBytes;
        if (LogHeadersMaxBytes != null) updates["log_headers_max_bytes"] = LogHeadersMaxBytes;
        if (LogRedactSensitiveHeaders != null) updates["log_redact_sensitive_headers"] = LogRedactSensitiveHeaders;
        if (InboundResponseMode != null) updates["inbound_response_mode"] = InboundResponseMode;
        if (InboundAuthMode != null) updates["inbound_auth_mode"] = InboundAuthMode;
        if (InboundAuthHeaderName != null) updates["inbound_auth_header_name"] = InboundAuthHeaderName;
        if (InboundAuthUsername != null) updates["inbound_auth_username"] = InboundAuthUsername;
        if (InboundAuthValueHash != null) updates["inbound_auth_value_hash"] = InboundAuthValueHash;
        if (InboundAuthValueLast4 != null) updates["inbound_auth_value_last4"] = InboundAuthValueLast4;
        if (InboundSecretHash != null) updates["inbound_secret_hash"] = InboundSecretHash;
        if (InboundSecretLast4 != null) updates["inbound_secret_last4"] = InboundSecretLast4;
        return updates;
    }
}
