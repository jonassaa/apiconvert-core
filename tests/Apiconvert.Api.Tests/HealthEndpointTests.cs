using System.Net;
using System.Text.Json;

namespace Apiconvert.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkStatus()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.GetProperty("status").GetString();
        Assert.Equal("ok", status);
    }
}
