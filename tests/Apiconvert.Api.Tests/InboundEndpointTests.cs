using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Apiconvert.Api.Converters;
using Apiconvert.Api.Inbound;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apiconvert.Api.Tests;

public sealed class InboundEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public InboundEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Inbound_WithoutPath_ReturnsBadRequest()
    {
        var orgId = Guid.NewGuid();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/inbound/{orgId}/");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var error = doc.RootElement.GetProperty("error").GetString();
        Assert.Equal("Inbound path is required", error);
        Assert.True(doc.RootElement.TryGetProperty("request_id", out _));
    }

    [Fact]
    public async Task Inbound_WithValidConverter_ForwardsResponse()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterConfig
        {
            Id = converterId,
            OrgId = orgId,
            Enabled = true,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = false
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();
            services.RemoveAll<IMappingRepository>();
            services.RemoveAll<IForwarder>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IConverterRepository>(new StubConverterRepository(converter));
            services.AddSingleton<IMappingRepository>(new StubMappingRepository(MinimalJsonMapping));
            services.AddSingleton<IForwarder>(new StubForwarder(new ForwardResult
            {
                StatusCode = 200,
                JsonBody = new { ok = true }
            }));
        });

        var response = await client.PostAsJsonAsync($"/api/inbound/{orgId}/orders", new { hello = "world" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("x-apiconvert-request-id"));
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var ok = doc.RootElement.GetProperty("ok").GetBoolean();
        Assert.True(ok);
    }

    [Fact]
    public async Task Inbound_WhenRateLimited_ReturnsRetryAfter()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterConfig
        {
            Id = converterId,
            OrgId = orgId,
            Enabled = true,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = false
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();
            services.RemoveAll<IRateLimiter>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IConverterRepository>(new StubConverterRepository(converter));
            services.AddSingleton<IRateLimiter>(new StubRateLimiter(true));
        });

        var response = await client.GetAsync($"/api/inbound/{orgId}/orders");

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Retry-After", out var values));
        Assert.Contains("60", values);
    }

    [Fact]
    public async Task Inbound_WhenConverterDisabled_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterConfig
        {
            Id = converterId,
            OrgId = orgId,
            Enabled = false,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = false
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IConverterRepository>(new StubConverterRepository(converter));
        });

        var response = await client.GetAsync($"/api/inbound/{orgId}/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var error = doc.RootElement.GetProperty("error").GetString();
        Assert.Equal("Converter disabled", error);
    }

    [Fact]
    public async Task Inbound_WhenBearerAuthMissing_ReturnsUnauthorized()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterConfig
        {
            Id = converterId,
            OrgId = orgId,
            Enabled = true,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = false,
            InboundAuthMode = "bearer",
            InboundAuthValueHash = HashSecret("secret-token")
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IConverterRepository>(new StubConverterRepository(converter));
        });

        var response = await client.GetAsync($"/api/inbound/{orgId}/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inbound_WhenBasicAuthMissing_ReturnsUnauthorized()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterConfig
        {
            Id = converterId,
            OrgId = orgId,
            Enabled = true,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = false,
            InboundAuthMode = "basic",
            InboundAuthUsername = "user",
            InboundAuthValueHash = HashSecret("password")
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IConverterRepository>(new StubConverterRepository(converter));
        });

        var response = await client.GetAsync($"/api/inbound/{orgId}/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inbound_WhenHeaderAuthMissing_ReturnsUnauthorized()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterConfig
        {
            Id = converterId,
            OrgId = orgId,
            Enabled = true,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = false,
            InboundAuthMode = "header",
            InboundAuthHeaderName = "x-custom-token",
            InboundAuthValueHash = HashSecret("secret")
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IConverterRepository>(new StubConverterRepository(converter));
        });

        var response = await client.GetAsync($"/api/inbound/{orgId}/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inbound_WhenPayloadTooLarge_ReturnsPayloadTooLarge()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterConfig
        {
            Id = converterId,
            OrgId = orgId,
            Enabled = true,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = false
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IConverterRepository>(new StubConverterRepository(converter));
        });

        var payload = new string('a', InboundConstants.MaxInboundBodyBytes + 1);
        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"/api/inbound/{orgId}/orders", content);

        Assert.Equal((HttpStatusCode)413, response.StatusCode);
    }

    private HttpClient CreateClientWithOverrides(Action<IServiceCollection> configureServices)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(configureServices);
        }).CreateClient();
    }

    private const string MinimalJsonMapping = """
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "json",
  "fieldMappings": [],
  "arrayMappings": []
}
""";

    private sealed class StubOrganizationRepository : IOrganizationRepository
    {
        private readonly bool _exists;

        public StubOrganizationRepository(bool exists)
        {
            _exists = exists;
        }

        public Task<bool> ExistsAsync(Guid orgId, CancellationToken cancellationToken) => Task.FromResult(_exists);
    }

    private sealed class StubConverterRepository : IConverterRepository
    {
        private readonly ConverterConfig? _converter;

        public StubConverterRepository(ConverterConfig? converter)
        {
            _converter = converter;
        }

        public Task<ConverterConfig?> GetByOrgAndPathAsync(Guid orgId, string inboundPath, CancellationToken cancellationToken)
            => Task.FromResult(_converter);
    }

    private sealed class StubMappingRepository : IMappingRepository
    {
        private readonly string? _mappingJson;

        public StubMappingRepository(string? mappingJson)
        {
            _mappingJson = mappingJson;
        }

        public Task<string?> GetLatestMappingJsonAsync(Guid converterId, CancellationToken cancellationToken)
            => Task.FromResult(_mappingJson);

        public Task<ConverterMappingSnapshot?> GetLatestMappingAsync(Guid converterId, CancellationToken cancellationToken)
            => Task.FromResult<ConverterMappingSnapshot?>(null);

        public Task<int> InsertMappingAsync(Guid converterId, string mappingJson, string? inputSample, string? outputSample, CancellationToken cancellationToken)
            => Task.FromResult(1);
    }

    private sealed class StubForwarder : IForwarder
    {
        private readonly ForwardResult _result;

        public StubForwarder(ForwardResult result)
        {
            _result = result;
        }

        public Task<ForwardResult> SendAsync(ForwardRequest request, CancellationToken cancellationToken)
            => Task.FromResult(_result);
    }

    private sealed class StubRateLimiter : IRateLimiter
    {
        private readonly bool _isLimited;

        public StubRateLimiter(bool isLimited)
        {
            _isLimited = isLimited;
        }

        public bool IsRateLimited(string key) => _isLimited;
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
