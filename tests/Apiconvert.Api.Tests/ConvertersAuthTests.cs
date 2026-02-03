using System.Net;
using System.Text.Json;

namespace Apiconvert.Api.Tests;

public sealed class ConvertersAuthTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ConvertersAuthTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetConverters_WithoutAuth_ReturnsUnauthorized()
    {
        var orgId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/orgs/{orgId}/converters");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConverters_WithAuth_ReturnsEmptyList()
    {
        var orgId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orgs/{orgId}/converters");
        request.Headers.Add("x-test-user", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var converters = doc.RootElement.GetProperty("converters");
        Assert.Equal(JsonValueKind.Array, converters.ValueKind);
        Assert.Empty(converters.EnumerateArray());
    }
}
