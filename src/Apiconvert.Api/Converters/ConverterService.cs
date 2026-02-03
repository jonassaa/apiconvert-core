using System.Security.Cryptography;
using System.Text.Json;
using Apiconvert.Api.Admin;
using Apiconvert.Api.Inbound;

namespace Apiconvert.Api.Converters;

public sealed class ConverterService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrgMembershipRepository _membershipRepository;
    private readonly IConverterQueryRepository _queryRepository;
    private readonly IConverterAdminRepository _adminRepository;
    private readonly IMappingRepository _mappingRepository;
    private readonly IConverterLogQueryRepository _logQueryRepository;

    public ConverterService(
        IOrganizationRepository organizationRepository,
        IOrgMembershipRepository membershipRepository,
        IConverterQueryRepository queryRepository,
        IConverterAdminRepository adminRepository,
        IMappingRepository mappingRepository,
        IConverterLogQueryRepository logQueryRepository)
    {
        _organizationRepository = organizationRepository;
        _membershipRepository = membershipRepository;
        _queryRepository = queryRepository;
        _adminRepository = adminRepository;
        _mappingRepository = mappingRepository;
        _logQueryRepository = logQueryRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<ConverterSummary>>> ListAsync(
        Guid userId,
        Guid orgId,
        string? search,
        bool? enabled,
        bool? logging,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role))
        {
            return ServiceResult<IReadOnlyList<ConverterSummary>>.Fail("Not a member", "forbidden");
        }

        var converters = await _queryRepository.ListAsync(orgId, search, enabled, logging, cancellationToken);
        return ServiceResult<IReadOnlyList<ConverterSummary>>.Success(converters);
    }

    public async Task<ServiceResult<ConverterDetailBundle>> GetDetailByNameAsync(
        Guid userId,
        Guid orgId,
        string name,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role))
        {
            return ServiceResult<ConverterDetailBundle>.Fail("Not a member", "forbidden");
        }

        var normalizedName = NormalizeNameForLookup(name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return ServiceResult<ConverterDetailBundle>.Fail("Converter name is required.");
        }

        var converter = await _queryRepository.GetByNameAsync(orgId, normalizedName, cancellationToken);
        if (converter == null)
        {
            return ServiceResult<ConverterDetailBundle>.Fail("Converter not found", "not_found");
        }

        var mapping = await _mappingRepository.GetLatestMappingAsync(converter.Id, cancellationToken);
        var logs = await _logQueryRepository.GetRecentAsync(converter.Id, 10, cancellationToken);

        return ServiceResult<ConverterDetailBundle>.Success(new ConverterDetailBundle
        {
            Converter = converter,
            Mapping = mapping,
            Logs = logs
        });
    }

    public async Task<ServiceResult<Guid>> CreateAsync(
        Guid userId,
        ConverterCreateRequest request,
        CancellationToken cancellationToken)
    {
        var orgExists = await _organizationRepository.ExistsAsync(request.OrgId, cancellationToken);
        if (!orgExists)
        {
            return ServiceResult<Guid>.Fail("Organization not found", "org_not_found");
        }

        var role = await _membershipRepository.GetRoleAsync(request.OrgId, userId, cancellationToken);
        if (!IsAdminRole(role))
        {
            return ServiceResult<Guid>.Fail("Only admins can create converters", "forbidden");
        }

        var normalizedName = NormalizeName(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return ServiceResult<Guid>.Fail("Name is required.");
        }

        var lookupName = NormalizeNameForLookup(normalizedName);
        if (await _queryRepository.ExistsByNameAsync(request.OrgId, lookupName, null, cancellationToken))
        {
            return ServiceResult<Guid>.Fail("Name already in use");
        }

        var normalizedInboundPath = NormalizeInboundPath(request.InboundPath);
        if (string.IsNullOrWhiteSpace(normalizedInboundPath))
        {
            return ServiceResult<Guid>.Fail("Inbound path is required.");
        }

        if (await _queryRepository.ExistsByInboundPathAsync(request.OrgId, normalizedInboundPath, null, cancellationToken))
        {
            return ServiceResult<Guid>.Fail("Inbound path already in use");
        }

        var forwardValidation = ForwardUrlValidator.Validate(request.ForwardUrl);
        if (!forwardValidation.Ok)
        {
            return ServiceResult<Guid>.Fail(forwardValidation.Error ?? "Invalid forward URL");
        }

        var baseHeaders = ParseHeaders(request.ForwardHeadersJson);
        if (baseHeaders == null)
        {
            return ServiceResult<Guid>.Fail("Invalid headers JSON");
        }

        var outboundResult = ApplyOutboundAuth(
            baseHeaders,
            request.OutboundAuthMode ?? "none",
            request.OutboundAuthToken,
            request.OutboundAuthUsername,
            request.OutboundAuthPassword,
            allowKeepExisting: false);
        if (outboundResult == null)
        {
            return ServiceResult<Guid>.Fail("Outbound auth credentials required");
        }

        var outboundHeaders = outboundResult;
        var customHeaderName = NormalizeHeaderName(request.OutboundCustomHeaderName);
        var customHeaderValue = request.OutboundCustomHeaderValue?.Trim();
        if (HasHeaderMismatch(customHeaderName, customHeaderValue))
        {
            return ServiceResult<Guid>.Fail("Outbound custom header requires name and value");
        }
        if (!string.IsNullOrWhiteSpace(customHeaderName) && !string.IsNullOrWhiteSpace(customHeaderValue))
        {
            outboundHeaders[customHeaderName] = customHeaderValue;
        }

        var inboundAuthMode = request.InboundAuthMode ?? (string.IsNullOrWhiteSpace(request.InboundSecret) ? "none" : "bearer");
        var inboundAuth = ResolveInboundAuth(inboundAuthMode, request, null);
        if (!inboundAuth.Ok)
        {
            return ServiceResult<Guid>.Fail(inboundAuth.Error ?? "Invalid inbound auth");
        }

        var logBodyMaxBytes = request.LogBodyMaxKb.HasValue
            ? request.LogBodyMaxKb.Value * 1024
            : request.LogBodyMaxBytes;
        var logHeadersMaxBytes = request.LogHeadersMaxKb.HasValue
            ? request.LogHeadersMaxKb.Value * 1024
            : request.LogHeadersMaxBytes;

        var createRequest = new AdminConverterCreateRequest
        {
            OrgId = request.OrgId,
            Name = normalizedName,
            InboundPath = normalizedInboundPath,
            ForwardUrl = request.ForwardUrl,
            ForwardMethod = request.ForwardMethod,
            ForwardHeaders = outboundHeaders,
            Enabled = request.Enabled ?? true,
            LogRequestsEnabled = request.LogRequestsEnabled ?? true,
            InboundAuthMode = inboundAuthMode,
            InboundAuthHeaderName = inboundAuth.InboundAuthHeaderName,
            InboundAuthUsername = inboundAuth.InboundAuthUsername,
            InboundAuthValueHash = inboundAuth.InboundAuthValueHash,
            InboundAuthValueLast4 = inboundAuth.InboundAuthValueLast4,
            InboundSecretHash = inboundAuth.InboundSecretHash,
            InboundSecretLast4 = inboundAuth.InboundSecretLast4,
            LogRetentionDays = request.LogRetentionDays,
            LogBodyMaxBytes = logBodyMaxBytes,
            LogHeadersMaxBytes = logHeadersMaxBytes,
            LogRedactSensitiveHeaders = request.LogRedactSensitiveHeaders,
            InboundResponseMode = request.InboundResponseMode
        };

        var converterId = await _adminRepository.CreateAsync(createRequest, cancellationToken);
        if (converterId == Guid.Empty)
        {
            return ServiceResult<Guid>.Fail("Failed to create converter");
        }

        return ServiceResult<Guid>.Success(converterId);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(
        Guid userId,
        Guid orgId,
        Guid converterId,
        ConverterUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var orgExists = await _organizationRepository.ExistsAsync(orgId, cancellationToken);
        if (!orgExists)
        {
            return ServiceResult<bool>.Fail("Organization not found", "org_not_found");
        }

        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (!IsAdminRole(role))
        {
            return ServiceResult<bool>.Fail("Only admins can update converters", "forbidden");
        }

        var converter = await _queryRepository.GetByIdAsync(orgId, converterId, cancellationToken);
        if (converter == null)
        {
            return ServiceResult<bool>.Fail("Converter not found", "not_found");
        }

        var updates = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var nextName = NormalizeName(request.Name);
            if (string.IsNullOrWhiteSpace(nextName))
            {
                return ServiceResult<bool>.Fail("Name is required.");
            }
            var lookupName = NormalizeNameForLookup(nextName);
            if (await _queryRepository.ExistsByNameAsync(orgId, lookupName, converterId, cancellationToken))
            {
                return ServiceResult<bool>.Fail("Name already in use");
            }
            updates["name"] = nextName;
        }

        if (!string.IsNullOrWhiteSpace(request.InboundPath))
        {
            var inboundPath = NormalizeInboundPath(request.InboundPath);
            if (string.IsNullOrWhiteSpace(inboundPath))
            {
                return ServiceResult<bool>.Fail("Inbound path is required.");
            }
            if (await _queryRepository.ExistsByInboundPathAsync(orgId, inboundPath, converterId, cancellationToken))
            {
                return ServiceResult<bool>.Fail("Inbound path already in use");
            }
            updates["inbound_path"] = inboundPath;
        }

        if (!string.IsNullOrWhiteSpace(request.ForwardUrl))
        {
            var validation = ForwardUrlValidator.Validate(request.ForwardUrl);
            if (!validation.Ok)
            {
                return ServiceResult<bool>.Fail(validation.Error ?? "Invalid forward URL");
            }
            updates["forward_url"] = request.ForwardUrl;
        }

        if (!string.IsNullOrWhiteSpace(request.ForwardMethod))
        {
            updates["forward_method"] = request.ForwardMethod;
        }

        Dictionary<string, string>? parsedHeaders = null;
        if (request.ForwardHeadersJson != null)
        {
            var parsed = ParseHeaders(request.ForwardHeadersJson);
            if (parsed == null)
            {
                return ServiceResult<bool>.Fail("Invalid headers JSON");
            }
            parsedHeaders = parsed;
            updates["forward_headers_json"] = parsedHeaders;
        }

        if (request.Enabled.HasValue)
        {
            updates["enabled"] = request.Enabled.Value;
        }
        if (request.LogRequestsEnabled.HasValue)
        {
            updates["log_requests_enabled"] = request.LogRequestsEnabled.Value;
        }
        if (request.LogRetentionDays.HasValue)
        {
            updates["log_retention_days"] = request.LogRetentionDays.Value;
        }
        if (request.LogBodyMaxKb.HasValue)
        {
            updates["log_body_max_bytes"] = request.LogBodyMaxKb.Value * 1024;
        }
        else if (request.LogBodyMaxBytes.HasValue)
        {
            updates["log_body_max_bytes"] = request.LogBodyMaxBytes.Value;
        }
        if (request.LogHeadersMaxKb.HasValue)
        {
            updates["log_headers_max_bytes"] = request.LogHeadersMaxKb.Value * 1024;
        }
        else if (request.LogHeadersMaxBytes.HasValue)
        {
            updates["log_headers_max_bytes"] = request.LogHeadersMaxBytes.Value;
        }
        if (request.LogRedactSensitiveHeaders.HasValue)
        {
            updates["log_redact_sensitive_headers"] = request.LogRedactSensitiveHeaders.Value;
        }
        if (!string.IsNullOrWhiteSpace(request.InboundResponseMode))
        {
            updates["inbound_response_mode"] = request.InboundResponseMode;
        }

        var inboundMode = request.InboundAuthMode
            ?? converter.InboundAuthMode
            ?? (string.IsNullOrWhiteSpace(converter.InboundSecretHash) ? "none" : "bearer");

        updates["inbound_auth_mode"] = inboundMode;
        var inboundAuth = ResolveInboundAuth(inboundMode, request, converter);
        if (!inboundAuth.Ok)
        {
            return ServiceResult<bool>.Fail(inboundAuth.Error ?? "Invalid inbound auth");
        }

        updates["inbound_auth_header_name"] = inboundAuth.InboundAuthHeaderName;
        updates["inbound_auth_username"] = inboundAuth.InboundAuthUsername;
        updates["inbound_auth_value_hash"] = inboundAuth.InboundAuthValueHash;
        updates["inbound_auth_value_last4"] = inboundAuth.InboundAuthValueLast4;
        updates["inbound_secret_hash"] = inboundAuth.InboundSecretHash;
        updates["inbound_secret_last4"] = inboundAuth.InboundSecretLast4;

        var customHeaderName = NormalizeHeaderName(request.OutboundCustomHeaderName);
        var customHeaderValue = request.OutboundCustomHeaderValue?.Trim();
        if (HasHeaderMismatch(customHeaderName, customHeaderValue))
        {
            return ServiceResult<bool>.Fail("Outbound custom header requires name and value");
        }

        if (!string.IsNullOrWhiteSpace(request.OutboundAuthMode) ||
            !string.IsNullOrWhiteSpace(customHeaderName))
        {
            var baseHeaders = parsedHeaders ?? new Dictionary<string, string>(converter.ForwardHeaders);
            var outboundHeaders = baseHeaders;
            if (!string.IsNullOrWhiteSpace(request.OutboundAuthMode))
            {
                var resolved = ApplyOutboundAuth(
                    baseHeaders,
                    request.OutboundAuthMode,
                    request.OutboundAuthToken,
                    request.OutboundAuthUsername,
                    request.OutboundAuthPassword,
                    allowKeepExisting: true);
                if (resolved == null)
                {
                    return ServiceResult<bool>.Fail("Outbound auth credentials required");
                }
                outboundHeaders = resolved;
            }
            if (!string.IsNullOrWhiteSpace(customHeaderName) && !string.IsNullOrWhiteSpace(customHeaderValue))
            {
                outboundHeaders = new Dictionary<string, string>(outboundHeaders)
                {
                    [customHeaderName] = customHeaderValue
                };
            }
            updates["forward_headers_json"] = outboundHeaders;
        }

        var updated = await _adminRepository.UpdateAsync(orgId, converterId, updates, cancellationToken);
        return updated
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Converter not found", "not_found");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid userId,
        Guid orgId,
        Guid converterId,
        CancellationToken cancellationToken)
    {
        var orgExists = await _organizationRepository.ExistsAsync(orgId, cancellationToken);
        if (!orgExists)
        {
            return ServiceResult<bool>.Fail("Organization not found", "org_not_found");
        }

        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (!IsAdminRole(role))
        {
            return ServiceResult<bool>.Fail("Only admins can delete converters", "forbidden");
        }

        var deleted = await _adminRepository.DeleteAsync(orgId, converterId, cancellationToken);
        return deleted
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Converter not found", "not_found");
    }

    public async Task<ServiceResult<int>> SaveMappingAsync(
        Guid userId,
        Guid orgId,
        Guid converterId,
        string mappingJson,
        string? inputSample,
        string? outputSample,
        CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (!IsAdminRole(role))
        {
            return ServiceResult<int>.Fail("Only admins can update mappings", "forbidden");
        }

        var converter = await _queryRepository.GetByIdAsync(orgId, converterId, cancellationToken);
        if (converter == null)
        {
            return ServiceResult<int>.Fail("Converter not found", "not_found");
        }

        var normalizedMapping = NormalizeMappingJson(mappingJson);
        if (normalizedMapping == null)
        {
            return ServiceResult<int>.Fail("Invalid mapping JSON");
        }

        var normalizedInput = NormalizeSample(inputSample);
        var normalizedOutput = NormalizeSample(outputSample);
        var version = await _mappingRepository.InsertMappingAsync(
            converterId,
            normalizedMapping,
            normalizedInput,
            normalizedOutput,
            cancellationToken);

        return ServiceResult<int>.Success(version);
    }

    private static string NormalizeName(string name)
    {
        return name.Trim();
    }

    private static string NormalizeNameForLookup(string name)
    {
        return NormalizeName(name).ToLowerInvariant();
    }

    private static string NormalizeInboundPath(string path)
    {
        return path.Trim().Trim('/').Trim();
    }

    private static bool IsAdminRole(string? role)
    {
        return role == "owner" || role == "admin";
    }

    private static string? NormalizeHeaderName(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool HasHeaderMismatch(string? name, string? value)
    {
        return (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(value)) ||
               (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value));
    }

    private static Dictionary<string, string>? ParseHeaders(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            if (parsed == null)
            {
                return new Dictionary<string, string>();
            }
            var normalized = new Dictionary<string, string>();
            foreach (var entry in parsed)
            {
                if (entry.Value == null) continue;
                normalized[entry.Key] = entry.Value.ToString() ?? string.Empty;
            }
            return normalized;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string>? ApplyOutboundAuth(
        Dictionary<string, string> headers,
        string mode,
        string? token,
        string? username,
        string? password,
        bool allowKeepExisting)
    {
        var nextHeaders = new Dictionary<string, string>(headers);
        var existingAuth = GetAuthHeader(nextHeaders);

        if (mode == "none")
        {
            if (existingAuth != null)
            {
                nextHeaders.Remove(existingAuth.Value.Key);
            }
            return nextHeaders;
        }

        if (mode == "bearer")
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                nextHeaders["Authorization"] = $"Bearer {token}";
                return nextHeaders;
            }
            if (allowKeepExisting && existingAuth != null && existingAuth.Value.Value.StartsWith("Bearer "))
            {
                return nextHeaders;
            }
            return null;
        }

        if (mode == "basic")
        {
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                nextHeaders["Authorization"] = $"Basic {encoded}";
                return nextHeaders;
            }
            if (allowKeepExisting && existingAuth != null && existingAuth.Value.Value.StartsWith("Basic "))
            {
                return nextHeaders;
            }
            return null;
        }

        return nextHeaders;
    }

    private static KeyValuePair<string, string>? GetAuthHeader(Dictionary<string, string> headers)
    {
        foreach (var entry in headers)
        {
            if (entry.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }
        return null;
    }

    private static (bool Ok, string? Error,
        string? InboundAuthHeaderName,
        string? InboundAuthUsername,
        string? InboundAuthValueHash,
        string? InboundAuthValueLast4,
        string? InboundSecretHash,
        string? InboundSecretLast4) ResolveInboundAuth(
        string inboundMode,
        ConverterCreateRequest request,
        ConverterDetail? existing)
    {
        return inboundMode switch
        {
            "none" => (true, null, null, null, null, null, null, null),
            "bearer" => ResolveInboundBearer(
                request.InboundAuthToken ?? request.InboundSecret,
                existing),
            "basic" => ResolveInboundBasic(
                request.InboundAuthUsername,
                request.InboundAuthPassword,
                existing),
            "header" => ResolveInboundHeader(
                request.InboundAuthHeaderName,
                request.InboundAuthHeaderValue,
                existing),
            _ => (false, "Invalid inbound auth mode", null, null, null, null, null, null)
        };
    }

    private static (bool Ok, string? Error,
        string? InboundAuthHeaderName,
        string? InboundAuthUsername,
        string? InboundAuthValueHash,
        string? InboundAuthValueLast4,
        string? InboundSecretHash,
        string? InboundSecretLast4) ResolveInboundAuth(
        string inboundMode,
        ConverterUpdateRequest request,
        ConverterDetail? existing)
    {
        return inboundMode switch
        {
            "none" => (true, null, null, null, null, null, null, null),
            "bearer" => ResolveInboundBearer(
                request.InboundAuthToken ?? request.InboundSecret,
                existing),
            "basic" => ResolveInboundBasic(
                request.InboundAuthUsername,
                request.InboundAuthPassword,
                existing),
            "header" => ResolveInboundHeader(
                request.InboundAuthHeaderName,
                request.InboundAuthHeaderValue,
                existing),
            _ => (false, "Invalid inbound auth mode", null, null, null, null, null, null)
        };
    }

    private static (bool Ok, string? Error,
        string? InboundAuthHeaderName,
        string? InboundAuthUsername,
        string? InboundAuthValueHash,
        string? InboundAuthValueLast4,
        string? InboundSecretHash,
        string? InboundSecretLast4) ResolveInboundBearer(
        string? token,
        ConverterDetail? existing)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var hash = HashSecret(token);
            var last4 = GetLast4(token);
            return (true, null, null, null, hash, last4, hash, last4);
        }

        var existingHash = existing?.InboundAuthValueHash ?? existing?.InboundSecretHash;
        var existingLast4 = existing?.InboundAuthValueLast4 ?? existing?.InboundSecretLast4;

        if (string.IsNullOrWhiteSpace(existingHash))
        {
            return (false, "Inbound auth token required", null, null, null, null, null, null);
        }

        return (true, null, null, null, existingHash, existingLast4, existingHash, existingLast4);
    }

    private static (bool Ok, string? Error,
        string? InboundAuthHeaderName,
        string? InboundAuthUsername,
        string? InboundAuthValueHash,
        string? InboundAuthValueLast4,
        string? InboundSecretHash,
        string? InboundSecretLast4) ResolveInboundBasic(
        string? username,
        string? password,
        ConverterDetail? existing)
    {
        var resolvedUsername = !string.IsNullOrWhiteSpace(username)
            ? username.Trim()
            : existing?.InboundAuthUsername;
        if (string.IsNullOrWhiteSpace(resolvedUsername))
        {
            return (false, "Inbound basic username required", null, null, null, null, null, null);
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            var hash = HashSecret(password);
            var last4 = GetLast4(password);
            return (true, null, null, resolvedUsername, hash, last4, null, null);
        }

        if (string.IsNullOrWhiteSpace(existing?.InboundAuthValueHash))
        {
            return (false, "Inbound basic password required", null, null, null, null, null, null);
        }

        return (true, null, null, resolvedUsername, existing.InboundAuthValueHash, existing.InboundAuthValueLast4, null, null);
    }

    private static (bool Ok, string? Error,
        string? InboundAuthHeaderName,
        string? InboundAuthUsername,
        string? InboundAuthValueHash,
        string? InboundAuthValueLast4,
        string? InboundSecretHash,
        string? InboundSecretLast4) ResolveInboundHeader(
        string? headerName,
        string? headerValue,
        ConverterDetail? existing)
    {
        var resolvedHeaderName = !string.IsNullOrWhiteSpace(headerName)
            ? headerName.Trim()
            : existing?.InboundAuthHeaderName;
        if (string.IsNullOrWhiteSpace(resolvedHeaderName))
        {
            return (false, "Inbound header name required", null, null, null, null, null, null);
        }

        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            var hash = HashSecret(headerValue);
            var last4 = GetLast4(headerValue);
            return (true, null, resolvedHeaderName, null, hash, last4, null, null);
        }

        if (string.IsNullOrWhiteSpace(existing?.InboundAuthValueHash))
        {
            return (false, "Inbound header value required", null, null, null, null, null, null);
        }

        return (true, null, resolvedHeaderName, null, existing.InboundAuthValueHash, existing.InboundAuthValueLast4, null, null);
    }

    private static string HashSecret(string value)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetLast4(string value)
    {
        return value.Length <= 4 ? value : value[^4..];
    }

    private static string? NormalizeMappingJson(string mappingJson)
    {
        if (string.IsNullOrWhiteSpace(mappingJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(mappingJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                return doc.RootElement.GetRawText();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? NormalizeSample(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return value.Replace("\r\n", "\n");
    }
}
