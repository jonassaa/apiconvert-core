using Apiconvert.Api.Inbound;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/inbound/{orgId:guid}/{*path}")]
public sealed class InboundController : ControllerBase
{
    private readonly InboundHandler _handler;

    public InboundController(InboundHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    public async Task<IActionResult> Handle([FromRoute] Guid orgId, [FromRoute] string? path, CancellationToken cancellationToken)
    {
        var inboundPath = (path ?? string.Empty).Trim().Trim('/');
        var requestBody = await ReadRequestBodyAsync();

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var inboundRequest = new InboundRequest
        {
            OrgId = orgId,
            InboundPath = inboundPath,
            Method = Request.Method,
            Url = Request.GetEncodedUrl(),
            QueryString = Request.QueryString.Value ?? string.Empty,
            Body = requestBody,
            Headers = headers,
            SourceIp = Request.Headers["x-forwarded-for"].FirstOrDefault(),
            ContentLength = Request.ContentLength
        };

        var response = await _handler.HandleAsync(inboundRequest, cancellationToken);
        foreach (var header in response.Headers)
        {
            Response.Headers[header.Key] = header.Value;
        }

        if (response.JsonBody != null)
        {
            return new JsonResult(response.JsonBody) { StatusCode = response.StatusCode };
        }

        var result = Content(
            response.TextBody ?? string.Empty,
            response.ContentType ?? "text/plain",
            System.Text.Encoding.UTF8);
        result.StatusCode = response.StatusCode;
        return result;
    }

    private async Task<string> ReadRequestBodyAsync()
    {
        if (Request.ContentLength == null || Request.ContentLength == 0)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }
}
