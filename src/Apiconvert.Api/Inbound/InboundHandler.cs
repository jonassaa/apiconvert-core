using System.Security.Cryptography;
using System.Text;
using Apiconvert.Api.Converters;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Api.Inbound;

public sealed class InboundHandler
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IConverterRepository _converterRepository;
    private readonly IMappingRepository _mappingRepository;
    private readonly IConverterLogRepository _logRepository;
    private readonly IForwarder _forwarder;
    private readonly IRateLimiter _rateLimiter;

    public InboundHandler(
        IOrganizationRepository organizationRepository,
        IConverterRepository converterRepository,
        IMappingRepository mappingRepository,
        IConverterLogRepository logRepository,
        IForwarder forwarder,
        IRateLimiter rateLimiter)
    {
        _organizationRepository = organizationRepository;
        _converterRepository = converterRepository;
        _mappingRepository = mappingRepository;
        _logRepository = logRepository;
        _forwarder = forwarder;
        _rateLimiter = rateLimiter;
    }

    public async Task<InboundResponse> HandleAsync(InboundRequest request, CancellationToken cancellationToken)
    {
        if (!InboundConstants.AllowedMethods.Contains(request.Method))
        {
            return JsonResponse(405, new { error = "Method not allowed" });
        }

        var requestId = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(request.InboundPath))
        {
            return JsonResponse(400, new { error = "Inbound path is required", request_id = requestId });
        }

        if (!await _organizationRepository.ExistsAsync(request.OrgId, cancellationToken))
        {
            return JsonResponse(404, new { error = "Organization not found", request_id = requestId });
        }

        var converter = await _converterRepository.GetByOrgAndPathAsync(request.OrgId, request.InboundPath, cancellationToken);
        if (converter == null)
        {
            return JsonResponse(404, new { error = "Converter not found", request_id = requestId });
        }

        if (!converter.Enabled)
        {
            return JsonResponse(403, new { error = "Converter disabled", request_id = requestId });
        }

        if (request.ContentLength.HasValue && request.ContentLength.Value > InboundConstants.MaxInboundBodyBytes)
        {
            return JsonResponse(413, new { error = "Payload too large", request_id = requestId });
        }

        if (_rateLimiter.IsRateLimited(converter.Id.ToString()))
        {
            return JsonResponse(429, new { error = "Rate limit exceeded", request_id = requestId }, new Dictionary<string, string>
            {
                ["Retry-After"] = "60"
            });
        }

        var forwardValidation = ForwardUrlValidator.Validate(converter.ForwardUrl);
        if (!forwardValidation.Ok)
        {
            return JsonResponse(400, new { error = forwardValidation.Error ?? "Invalid forward URL", request_id = requestId });
        }

        var inboundAuthMode = converter.InboundAuthMode ?? (converter.InboundSecretHash != null ? "bearer" : "none");
        var inboundAuthHash = converter.InboundAuthValueHash ?? converter.InboundSecretHash;
        if (!string.Equals(inboundAuthMode, "none", StringComparison.OrdinalIgnoreCase))
        {
            var authorized = IsAuthorized(request, converter, inboundAuthMode, inboundAuthHash);
            if (!authorized)
            {
                return JsonResponse(401, new { error = "Unauthorized", request_id = requestId });
            }
        }

        var mappingJson = await _mappingRepository.GetLatestMappingJsonAsync(converter.Id, cancellationToken);
        var rules = ConversionEngine.NormalizeConversionRules(mappingJson == null ? null : mappingJson);

        var rawBody = rules.InputFormat == DataFormat.Query ? request.QueryString : request.Body;
        if (Encoding.UTF8.GetByteCount(rawBody) > InboundConstants.MaxInboundBodyBytes)
        {
            return JsonResponse(413, new { error = "Payload too large", request_id = requestId });
        }

        var (parsedValue, parseError) = ConversionEngine.ParsePayload(rawBody, rules.InputFormat);
        if (parseError != null)
        {
            var message = rules.InputFormat switch
            {
                DataFormat.Xml => "Invalid XML payload",
                DataFormat.Query => "Invalid query payload",
                _ => "Invalid JSON payload"
            };
            return JsonResponse(400, new { error = message, request_id = requestId });
        }

        var conversionResult = ConversionEngine.ApplyConversion(parsedValue, rules);
        if (conversionResult.Errors.Count > 0)
        {
            return JsonResponse(400, new { error = "Conversion rules error", details = conversionResult.Errors, request_id = requestId });
        }

        string outputBody;
        try
        {
            outputBody = ConversionEngine.FormatPayload(conversionResult.Output, rules.OutputFormat, false);
        }
        catch (Exception ex)
        {
            return JsonResponse(400, new { error = ex.Message, request_id = requestId });
        }

        var transformedLogPayload = rules.OutputFormat == DataFormat.Xml || rules.OutputFormat == DataFormat.Query
            ? outputBody
            : conversionResult.Output;

        var forwardHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = rules.OutputFormat switch
            {
                DataFormat.Xml => "application/xml",
                DataFormat.Query => "application/x-www-form-urlencoded",
                _ => "application/json"
            }
        };

        foreach (var entry in converter.ForwardHeaders)
        {
            forwardHeaders[entry.Key] = entry.Value;
        }

        var forwardMethod = (converter.ForwardMethod ?? request.Method).ToUpperInvariant();
        var forwardUrl = converter.ForwardUrl;
        var forwardBody = outputBody;

        if (rules.OutputFormat == DataFormat.Query)
        {
            if (!string.IsNullOrWhiteSpace(outputBody))
            {
                var url = new Uri(converter.ForwardUrl);
                var builder = new UriBuilder(url);
                var query = builder.Query.TrimStart('?');
                var combined = string.IsNullOrWhiteSpace(query)
                    ? outputBody
                    : string.Join("&", new[] { query, outputBody }.Where(s => !string.IsNullOrWhiteSpace(s)));
                builder.Query = combined;
                forwardUrl = builder.Uri.ToString();
            }
            forwardBody = null;
        }

        var forwardResult = await _forwarder.SendAsync(new ForwardRequest
        {
            Url = forwardUrl,
            Method = forwardMethod,
            Headers = forwardHeaders,
            Body = forwardBody
        }, cancellationToken);

        if (converter.LogRequestsEnabled)
        {
            var logBodyMaxBytes = converter.LogBodyMaxBytes ?? InboundConstants.DefaultLogBodyMaxBytes;
            var logHeadersMaxBytes = converter.LogHeadersMaxBytes ?? InboundConstants.DefaultLogHeadersMaxBytes;
            var redactSensitiveHeaders = converter.LogRedactSensitiveHeaders ?? true;

            var requestHeaders = RedactHeaders(request.Headers, redactSensitiveHeaders);
            var responseHeaders = forwardResult.Headers.Count > 0
                ? RedactHeaders(forwardResult.Headers, redactSensitiveHeaders)
                : null;

            var queryParsed = ConversionEngine.ParsePayload(request.QueryString, DataFormat.Query).Value;
            await _logRepository.InsertAsync(new ConverterLogEntry
            {
                ConverterId = converter.Id,
                OrgId = converter.OrgId,
                ReceivedAt = DateTimeOffset.UtcNow,
                RequestId = requestId,
                SourceIp = request.SourceIp,
                Method = request.Method,
                Path = new Uri(request.Url).AbsolutePath,
                HeadersJson = CapJsonPayload(requestHeaders, logHeadersMaxBytes),
                QueryJson = CapJsonPayload(queryParsed, logHeadersMaxBytes),
                BodyJson = CapJsonPayload(parsedValue, logBodyMaxBytes),
                TransformedBodyJson = CapJsonPayload(transformedLogPayload, logBodyMaxBytes),
                ForwardUrl = converter.ForwardUrl,
                ForwardStatus = forwardResult.StatusCode == 0 ? null : forwardResult.StatusCode,
                ForwardResponseMs = forwardResult.DurationMs,
                ErrorText = forwardResult.Error,
                ForwardResponseHeadersJson = responseHeaders == null ? null : CapJsonPayload(responseHeaders, logHeadersMaxBytes),
                ForwardResponseBodyJson = CapJsonPayload(forwardResult.JsonBody, logBodyMaxBytes),
                ForwardResponseBodyText = CapString(forwardResult.TextBody, logBodyMaxBytes)
            }, cancellationToken);

            var retentionDays = converter.LogRetentionDays ?? 30;
            if (retentionDays > 0)
            {
                await _logRepository.CleanupAsync(converter.Id, retentionDays, cancellationToken);
            }
        }

        if (forwardResult.Error != null)
        {
            return JsonResponse(502, new { error = forwardResult.Error, request_id = requestId });
        }

        var responseHeaderValues = new Dictionary<string, string>
        {
            ["x-apiconvert-request-id"] = requestId.ToString()
        };

        if (string.Equals(converter.InboundResponseMode, "ack", StringComparison.OrdinalIgnoreCase))
        {
            return JsonResponse(202, new { ok = true, request_id = requestId }, responseHeaderValues);
        }

        if (forwardResult.JsonBody != null)
        {
            return new InboundResponse
            {
                StatusCode = forwardResult.StatusCode == 0 ? 200 : forwardResult.StatusCode,
                JsonBody = forwardResult.JsonBody,
                Headers = responseHeaderValues
            };
        }

        return new InboundResponse
        {
            StatusCode = forwardResult.StatusCode == 0 ? 200 : forwardResult.StatusCode,
            TextBody = forwardResult.TextBody ?? string.Empty,
            ContentType = forwardResult.ContentType,
            Headers = responseHeaderValues
        };
    }

    private static bool IsAuthorized(InboundRequest request, ConverterConfig converter, string mode, string? hash)
    {
        if (hash == null) return false;
        if (string.Equals(mode, "bearer", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryGetValue("authorization", out var authHeader);
            var bearerToken = authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : null;
            request.Headers.TryGetValue("x-apiconvert-token", out var legacyToken);
            var token = bearerToken ?? legacyToken;
            return token != null && HashSecret(token) == hash;
        }

        if (string.Equals(mode, "basic", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryGetValue("authorization", out var authHeader);
            var basicToken = authHeader != null && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Basic ".Length..].Trim()
                : null;
            if (basicToken == null || converter.InboundAuthUsername == null)
            {
                return false;
            }
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(basicToken));
                var parts = decoded.Split(':');
                if (parts.Length < 2) return false;
                var username = parts[0];
                var password = string.Join(':', parts.Skip(1));
                return username == converter.InboundAuthUsername && HashSecret(password) == hash;
            }
            catch
            {
                return false;
            }
        }

        if (string.Equals(mode, "header", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(converter.InboundAuthHeaderName)) return false;
            request.Headers.TryGetValue(converter.InboundAuthHeaderName, out var headerValue);
            return headerValue != null && HashSecret(headerValue) == hash;
        }

        return false;
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static Dictionary<string, string> RedactHeaders(Dictionary<string, string> headers, bool enabled)
    {
        if (!enabled) return headers;
        return headers.ToDictionary(
            entry => entry.Key,
            entry => InboundConstants.SensitiveHeaders.Contains(entry.Key)
                ? "[REDACTED]"
                : entry.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static object? CapJsonPayload(object? value, int maxBytes)
    {
        if (value == null) return value;
        try
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(value);
            var size = Encoding.UTF8.GetByteCount(serialized);
            if (size <= maxBytes) return value;
            return new
            {
                _truncated = true,
                size,
                preview = serialized[..Math.Min(serialized.Length, maxBytes)]
            };
        }
        catch
        {
            return new { _truncated = true, preview = "[unserializable]" };
        }
    }

    private static string? CapString(string? value, int maxBytes)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var size = Encoding.UTF8.GetByteCount(value);
        if (size <= maxBytes) return value;
        return value[..Math.Min(value.Length, maxBytes)] + "…";
    }

    private static InboundResponse JsonResponse(int status, object body, Dictionary<string, string>? headers = null)
    {
        return new InboundResponse
        {
            StatusCode = status,
            JsonBody = body,
            Headers = headers ?? new Dictionary<string, string>()
        };
    }
}
