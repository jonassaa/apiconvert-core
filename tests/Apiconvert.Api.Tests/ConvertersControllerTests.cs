using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Apiconvert.Api.Admin;
using Apiconvert.Api.Converters;
using Apiconvert.Api.Inbound;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apiconvert.Api.Tests;

public sealed class ConvertersControllerTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ConvertersControllerTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Lookup_WithMapping_ReturnsConverterDetail()
    {
        var orgId = Guid.NewGuid();
        var converterId = Guid.NewGuid();
        var converter = new ConverterDetail
        {
            Id = converterId,
            Name = "Orders",
            InboundPath = "orders",
            Enabled = true,
            ForwardUrl = "https://example.com/webhook",
            ForwardMethod = "POST",
            LogRequestsEnabled = true,
            ForwardHeaders = new Dictionary<string, string> { ["x-test"] = "value" }
        };
        var mappingJson = """
        { "version": 2, "inputFormat": "json", "outputFormat": "json", "fieldMappings": [], "arrayMappings": [] }
        """;
        var mapping = new ConverterMappingSnapshot
        {
            MappingJson = mappingJson,
            InputSample = "{\"hello\":\"world\"}",
            OutputSample = "{\"ok\":true}"
        };

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IConverterQueryRepository>();
            services.RemoveAll<IMappingRepository>();
            services.RemoveAll<IConverterLogQueryRepository>();

            services.AddSingleton<IConverterQueryRepository>(new StubConverterQueryRepository(converter));
            services.AddSingleton<IMappingRepository>(new StubMappingRepository(mapping));
            services.AddSingleton<IConverterLogQueryRepository>(new StubConverterLogQueryRepository());
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orgs/{orgId}/converters/lookup?name=Orders");
        request.Headers.Add("x-test-user", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var converterNode = doc.RootElement.GetProperty("converter");
        Assert.Equal(converterId, converterNode.GetProperty("id").GetGuid());
        Assert.Equal("Orders", converterNode.GetProperty("name").GetString());

        var mappingNode = doc.RootElement.GetProperty("mapping");
        Assert.Equal(2, mappingNode.GetProperty("version").GetInt32());
        Assert.Equal("{\"hello\":\"world\"}", doc.RootElement.GetProperty("inputSample").GetString());
        Assert.Equal("{\"ok\":true}", doc.RootElement.GetProperty("outputSample").GetString());
    }

    [Fact]
    public async Task Create_WithAdminRole_ReturnsId()
    {
        var orgId = Guid.NewGuid();
        var createdId = Guid.NewGuid();

        var client = CreateClientWithOverrides(services =>
        {
            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IOrgMembershipRepository>();
            services.RemoveAll<IConverterQueryRepository>();
            services.RemoveAll<IConverterAdminRepository>();

            services.AddSingleton<IOrganizationRepository>(new StubOrganizationRepository(true));
            services.AddSingleton<IOrgMembershipRepository>(new StubMembershipRepository("admin"));
            services.AddSingleton<IConverterQueryRepository>(new StubConverterQueryRepository(null));
            services.AddSingleton<IConverterAdminRepository>(new StubConverterAdminRepository(createdId));
        });

        var payload = new
        {
            name = "Orders",
            inboundPath = "orders",
            forwardUrl = "https://example.com/webhook",
            forwardHeadersJson = "{}"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/orgs/{orgId}/converters")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-test-user", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(createdId, doc.RootElement.GetProperty("id").GetGuid());
    }

    private HttpClient CreateClientWithOverrides(Action<IServiceCollection> configureServices)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(configureServices);
        }).CreateClient();
    }

    private sealed class StubConverterQueryRepository : IConverterQueryRepository
    {
        private readonly ConverterDetail? _detail;

        public StubConverterQueryRepository(ConverterDetail? detail)
        {
            _detail = detail;
        }

        public Task<IReadOnlyList<ConverterSummary>> ListAsync(Guid orgId, string? search, bool? enabled, bool? logging, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConverterSummary>>(Array.Empty<ConverterSummary>());

        public Task<ConverterDetail?> GetByNameAsync(Guid orgId, string normalizedName, CancellationToken cancellationToken)
            => Task.FromResult(_detail);

        public Task<ConverterDetail?> GetByIdAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
            => Task.FromResult(_detail);

        public Task<bool> ExistsByNameAsync(Guid orgId, string normalizedName, Guid? excludeId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<bool> ExistsByInboundPathAsync(Guid orgId, string inboundPath, Guid? excludeId, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }

    private sealed class StubMappingRepository : IMappingRepository
    {
        private readonly ConverterMappingSnapshot? _mapping;

        public StubMappingRepository(ConverterMappingSnapshot? mapping)
        {
            _mapping = mapping;
        }

        public Task<string?> GetLatestMappingJsonAsync(Guid converterId, CancellationToken cancellationToken)
            => Task.FromResult(_mapping?.MappingJson);

        public Task<ConverterMappingSnapshot?> GetLatestMappingAsync(Guid converterId, CancellationToken cancellationToken)
            => Task.FromResult(_mapping);

        public Task<int> InsertMappingAsync(Guid converterId, string mappingJson, string? inputSample, string? outputSample, CancellationToken cancellationToken)
            => Task.FromResult(1);
    }

    private sealed class StubConverterLogQueryRepository : IConverterLogQueryRepository
    {
        public Task<IReadOnlyList<ConverterLogSummary>> GetRecentAsync(Guid converterId, int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConverterLogSummary>>(Array.Empty<ConverterLogSummary>());
    }

    private sealed class StubMembershipRepository : IOrgMembershipRepository
    {
        private readonly string? _role;

        public StubMembershipRepository(string? role)
        {
            _role = role;
        }

        public Task<string?> GetRoleAsync(Guid orgId, Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(_role);
    }

    private sealed class StubOrganizationRepository : IOrganizationRepository
    {
        private readonly bool _exists;

        public StubOrganizationRepository(bool exists)
        {
            _exists = exists;
        }

        public Task<bool> ExistsAsync(Guid orgId, CancellationToken cancellationToken) => Task.FromResult(_exists);
    }

    private sealed class StubConverterAdminRepository : IConverterAdminRepository
    {
        private readonly Guid _createdId;

        public StubConverterAdminRepository(Guid createdId)
        {
            _createdId = createdId;
        }

        public Task<Guid> CreateAsync(AdminConverterCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(_createdId);

        public Task<bool> UpdateAsync(Guid orgId, Guid converterId, Dictionary<string, object?> updates, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> DeleteAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> ExistsAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}
