using System.Text.Json;
using Apiconvert.Api.Converters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/orgs/{orgId:guid}/converters")]
[Authorize]
public sealed class ConvertersController : ControllerBase
{
    private readonly ConverterService _service;

    public ConvertersController(ConverterService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromRoute] Guid orgId,
        [FromQuery] string? search,
        [FromQuery] bool? enabled,
        [FromQuery] bool? logging,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.ListAsync(userId.Value, orgId, search, enabled, logging, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to load converters");
        }

        return Ok(new { converters = result.Value ?? Array.Empty<ConverterSummary>() });
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup(
        [FromRoute] Guid orgId,
        [FromQuery] string name,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetDetailByNameAsync(userId.Value, orgId, name, cancellationToken);
        if (!result.Ok || result.Value == null)
        {
            return MapError(result.ErrorCode, result.Error ?? "Converter not found");
        }

        var mappingJson = result.Value.Mapping?.MappingJson;
        JsonElement? mapping = null;
        if (!string.IsNullOrWhiteSpace(mappingJson))
        {
            using var doc = JsonDocument.Parse(mappingJson);
            mapping = doc.RootElement.Clone();
        }

        var converter = result.Value.Converter;
        var response = new
        {
            converter = new
            {
                converter.Id,
                converter.Name,
                converter.InboundPath,
                converter.Enabled,
                converter.ForwardUrl,
                converter.ForwardMethod,
                converter.ForwardHeaders,
                converter.LogRequestsEnabled,
                converter.InboundSecretLast4,
                converter.InboundAuthMode,
                converter.InboundAuthHeaderName,
                converter.InboundAuthUsername,
                converter.InboundAuthValueLast4,
                converter.LogRetentionDays,
                converter.LogBodyMaxBytes,
                converter.LogHeadersMaxBytes,
                converter.LogRedactSensitiveHeaders,
                converter.InboundResponseMode
            },
            mapping,
            inputSample = result.Value.Mapping?.InputSample,
            outputSample = result.Value.Mapping?.OutputSample,
            logs = result.Value.Logs
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromRoute] Guid orgId,
        [FromBody] ConverterCreateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var request = dto.ToRequest(orgId);
        var result = await _service.CreateAsync(userId.Value, request, cancellationToken);
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

        var result = await _service.UpdateAsync(userId.Value, orgId, converterId, dto.ToRequest(), cancellationToken);
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

        var result = await _service.DeleteAsync(userId.Value, orgId, converterId, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to delete converter");
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{converterId:guid}/mappings")]
    public async Task<IActionResult> SaveMapping(
        [FromRoute] Guid orgId,
        [FromRoute] Guid converterId,
        [FromBody] ConverterMappingDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.SaveMappingAsync(
            userId.Value,
            orgId,
            converterId,
            dto.MappingJson ?? string.Empty,
            dto.InputSample,
            dto.OutputSample,
            cancellationToken);

        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to save mapping");
        }

        return Ok(new { version = result.Value });
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
    public string? ForwardHeadersJson { get; init; }
    public bool? Enabled { get; init; }
    public bool? LogRequestsEnabled { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthToken { get; init; }
    public string? InboundSecret { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthPassword { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthHeaderValue { get; init; }
    public string? OutboundAuthMode { get; init; }
    public string? OutboundAuthToken { get; init; }
    public string? OutboundAuthUsername { get; init; }
    public string? OutboundAuthPassword { get; init; }
    public string? OutboundCustomHeaderName { get; init; }
    public string? OutboundCustomHeaderValue { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogBodyMaxKb { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public int? LogHeadersMaxKb { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }

    public ConverterCreateRequest ToRequest(Guid orgId)
    {
        return new ConverterCreateRequest
        {
            OrgId = orgId,
            Name = Name,
            InboundPath = InboundPath,
            ForwardUrl = ForwardUrl,
            ForwardMethod = ForwardMethod,
            ForwardHeadersJson = ForwardHeadersJson,
            Enabled = Enabled,
            LogRequestsEnabled = LogRequestsEnabled,
            InboundAuthMode = InboundAuthMode,
            InboundAuthToken = InboundAuthToken,
            InboundSecret = InboundSecret,
            InboundAuthUsername = InboundAuthUsername,
            InboundAuthPassword = InboundAuthPassword,
            InboundAuthHeaderName = InboundAuthHeaderName,
            InboundAuthHeaderValue = InboundAuthHeaderValue,
            OutboundAuthMode = OutboundAuthMode,
            OutboundAuthToken = OutboundAuthToken,
            OutboundAuthUsername = OutboundAuthUsername,
            OutboundAuthPassword = OutboundAuthPassword,
            OutboundCustomHeaderName = OutboundCustomHeaderName,
            OutboundCustomHeaderValue = OutboundCustomHeaderValue,
            LogRetentionDays = LogRetentionDays,
            LogBodyMaxBytes = LogBodyMaxBytes,
            LogBodyMaxKb = LogBodyMaxKb,
            LogHeadersMaxBytes = LogHeadersMaxBytes,
            LogHeadersMaxKb = LogHeadersMaxKb,
            LogRedactSensitiveHeaders = LogRedactSensitiveHeaders,
            InboundResponseMode = InboundResponseMode
        };
    }
}

public sealed record ConverterUpdateDto
{
    public string? Name { get; init; }
    public string? InboundPath { get; init; }
    public string? ForwardUrl { get; init; }
    public string? ForwardMethod { get; init; }
    public string? ForwardHeadersJson { get; init; }
    public bool? Enabled { get; init; }
    public bool? LogRequestsEnabled { get; init; }
    public string? InboundAuthMode { get; init; }
    public string? InboundAuthToken { get; init; }
    public string? InboundSecret { get; init; }
    public string? InboundAuthUsername { get; init; }
    public string? InboundAuthPassword { get; init; }
    public string? InboundAuthHeaderName { get; init; }
    public string? InboundAuthHeaderValue { get; init; }
    public string? OutboundAuthMode { get; init; }
    public string? OutboundAuthToken { get; init; }
    public string? OutboundAuthUsername { get; init; }
    public string? OutboundAuthPassword { get; init; }
    public string? OutboundCustomHeaderName { get; init; }
    public string? OutboundCustomHeaderValue { get; init; }
    public int? LogRetentionDays { get; init; }
    public int? LogBodyMaxBytes { get; init; }
    public int? LogBodyMaxKb { get; init; }
    public int? LogHeadersMaxBytes { get; init; }
    public int? LogHeadersMaxKb { get; init; }
    public bool? LogRedactSensitiveHeaders { get; init; }
    public string? InboundResponseMode { get; init; }

    public ConverterUpdateRequest ToRequest()
    {
        return new ConverterUpdateRequest
        {
            Name = Name,
            InboundPath = InboundPath,
            ForwardUrl = ForwardUrl,
            ForwardMethod = ForwardMethod,
            ForwardHeadersJson = ForwardHeadersJson,
            Enabled = Enabled,
            LogRequestsEnabled = LogRequestsEnabled,
            InboundAuthMode = InboundAuthMode,
            InboundAuthToken = InboundAuthToken,
            InboundSecret = InboundSecret,
            InboundAuthUsername = InboundAuthUsername,
            InboundAuthPassword = InboundAuthPassword,
            InboundAuthHeaderName = InboundAuthHeaderName,
            InboundAuthHeaderValue = InboundAuthHeaderValue,
            OutboundAuthMode = OutboundAuthMode,
            OutboundAuthToken = OutboundAuthToken,
            OutboundAuthUsername = OutboundAuthUsername,
            OutboundAuthPassword = OutboundAuthPassword,
            OutboundCustomHeaderName = OutboundCustomHeaderName,
            OutboundCustomHeaderValue = OutboundCustomHeaderValue,
            LogRetentionDays = LogRetentionDays,
            LogBodyMaxBytes = LogBodyMaxBytes,
            LogBodyMaxKb = LogBodyMaxKb,
            LogHeadersMaxBytes = LogHeadersMaxBytes,
            LogHeadersMaxKb = LogHeadersMaxKb,
            LogRedactSensitiveHeaders = LogRedactSensitiveHeaders,
            InboundResponseMode = InboundResponseMode
        };
    }
}

public sealed record ConverterMappingDto
{
    public string? MappingJson { get; init; }
    public string? InputSample { get; init; }
    public string? OutputSample { get; init; }
}
