using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Apiconvert.Api.Inbound;

namespace Apiconvert.Infrastructure.Http;

public sealed class HttpForwarder : IForwarder
{
    private readonly HttpClient _httpClient;

    public HttpForwarder(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMilliseconds(InboundConstants.DefaultTimeoutMs);
    }

    public async Task<ForwardResult> SendAsync(ForwardRequest request, CancellationToken cancellationToken)
    {
        var start = DateTimeOffset.UtcNow;
        try
        {
            using var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);
            foreach (var header in request.Headers)
            {
                if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    message.Content ??= new StringContent(string.Empty, Encoding.UTF8);
                    message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (request.Body != null)
            {
                var contentType = request.Headers.TryGetValue("Content-Type", out var value)
                    ? value
                    : "application/json";
                message.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
            }

            var response = await _httpClient.SendAsync(message, cancellationToken);
            var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            var contentTypeHeader = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
            var headers = response.Headers.Concat(response.Content.Headers)
                .ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);

            if (contentTypeHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                object? jsonBody = null;
                try
                {
                    jsonBody = JsonSerializer.Deserialize<object>(text);
                }
                catch
                {
                    jsonBody = null;
                }
                return new ForwardResult
                {
                    StatusCode = (int)response.StatusCode,
                    Headers = headers,
                    JsonBody = jsonBody,
                    TextBody = jsonBody == null ? text : null,
                    ContentType = contentTypeHeader,
                    DurationMs = durationMs
                };
            }

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ForwardResult
            {
                StatusCode = (int)response.StatusCode,
                Headers = headers,
                TextBody = responseText,
                ContentType = contentTypeHeader,
                DurationMs = durationMs
            };
        }
        catch (Exception ex)
        {
            var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            return new ForwardResult
            {
                StatusCode = 0,
                Error = ex.Message,
                DurationMs = durationMs
            };
        }
    }
}
