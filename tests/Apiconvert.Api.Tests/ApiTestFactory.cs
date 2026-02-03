using System.Security.Claims;
using System.Text.Encodings.Web;
using Apiconvert.Api.Admin;
using Apiconvert.Api.Converters;
using Apiconvert.Api.Inbound;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Apiconvert.Api.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public ApiTestFactory()
    {
        Environment.SetEnvironmentVariable("APICONVERT_DB_CONNECTION", "Host=localhost;Database=apiconvert;Username=postgres;Password=postgres");
        Environment.SetEnvironmentVariable("OPENROUTER_SITE_URL", "http://localhost");
        Environment.SetEnvironmentVariable("OPENROUTER_APP_NAME", "apiconvert-tests");
        Environment.SetEnvironmentVariable("SUPABASE_URL", "http://localhost");
        Environment.SetEnvironmentVariable("SUPABASE_JWT_AUDIENCE", "authenticated");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APICONVERT_DB_CONNECTION"] = "Host=localhost;Database=apiconvert;Username=postgres;Password=postgres",
                ["OPENROUTER_SITE_URL"] = "http://localhost",
                ["OPENROUTER_APP_NAME"] = "apiconvert-tests",
                ["SUPABASE_URL"] = "http://localhost",
                ["SUPABASE_JWT_AUDIENCE"] = "authenticated"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.RemoveAll<IOrganizationRepository>();
            services.RemoveAll<IConverterRepository>();
            services.RemoveAll<IMappingRepository>();
            services.RemoveAll<IConverterLogRepository>();
            services.RemoveAll<IForwarder>();
            services.RemoveAll<IRateLimiter>();
            services.RemoveAll<IOrgMembershipRepository>();
            services.RemoveAll<IConverterQueryRepository>();
            services.RemoveAll<IConverterAdminRepository>();
            services.RemoveAll<IConverterLogQueryRepository>();

            services.AddSingleton<IOrganizationRepository, FakeOrganizationRepository>();
            services.AddSingleton<IConverterRepository, FakeConverterRepository>();
            services.AddSingleton<IMappingRepository, FakeMappingRepository>();
            services.AddSingleton<IConverterLogRepository, FakeConverterLogRepository>();
            services.AddSingleton<IForwarder, FakeForwarder>();
            services.AddSingleton<IRateLimiter, FakeRateLimiter>();
            services.AddSingleton<IOrgMembershipRepository, FakeOrgMembershipRepository>();
            services.AddSingleton<IConverterQueryRepository, FakeConverterQueryRepository>();
            services.AddSingleton<IConverterAdminRepository, FakeConverterAdminRepository>();
            services.AddSingleton<IConverterLogQueryRepository, FakeConverterLogQueryRepository>();
        });
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("x-test-user", out var rawValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var value = rawValue.ToString();
        if (!Guid.TryParse(value, out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid x-test-user header."));
        }

        var claims = new[]
        {
            new Claim("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class FakeOrganizationRepository : IOrganizationRepository
{
    public Task<bool> ExistsAsync(Guid orgId, CancellationToken cancellationToken) => Task.FromResult(true);
}

internal sealed class FakeConverterRepository : IConverterRepository
{
    public Task<ConverterConfig?> GetByOrgAndPathAsync(Guid orgId, string inboundPath, CancellationToken cancellationToken)
        => Task.FromResult<ConverterConfig?>(null);
}

internal sealed class FakeMappingRepository : IMappingRepository
{
    public Task<string?> GetLatestMappingJsonAsync(Guid converterId, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    public Task<ConverterMappingSnapshot?> GetLatestMappingAsync(Guid converterId, CancellationToken cancellationToken)
        => Task.FromResult<ConverterMappingSnapshot?>(null);

    public Task<int> InsertMappingAsync(Guid converterId, string mappingJson, string? inputSample, string? outputSample, CancellationToken cancellationToken)
        => Task.FromResult(1);
}

internal sealed class FakeConverterLogRepository : IConverterLogRepository
{
    public Task InsertAsync(ConverterLogEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CleanupAsync(Guid converterId, int retentionDays, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeForwarder : IForwarder
{
    public Task<ForwardResult> SendAsync(ForwardRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new ForwardResult { StatusCode = 200, JsonBody = new { ok = true } });
}

internal sealed class FakeRateLimiter : IRateLimiter
{
    public bool IsRateLimited(string key) => false;
}

internal sealed class FakeOrgMembershipRepository : IOrgMembershipRepository
{
    public Task<string?> GetRoleAsync(Guid orgId, Guid userId, CancellationToken cancellationToken)
        => Task.FromResult<string?>("member");
}

internal sealed class FakeConverterQueryRepository : IConverterQueryRepository
{
    public Task<IReadOnlyList<ConverterSummary>> ListAsync(Guid orgId, string? search, bool? enabled, bool? logging, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ConverterSummary>>(Array.Empty<ConverterSummary>());

    public Task<ConverterDetail?> GetByNameAsync(Guid orgId, string normalizedName, CancellationToken cancellationToken)
        => Task.FromResult<ConverterDetail?>(null);

    public Task<ConverterDetail?> GetByIdAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
        => Task.FromResult<ConverterDetail?>(null);

    public Task<bool> ExistsByNameAsync(Guid orgId, string normalizedName, Guid? excludeId, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> ExistsByInboundPathAsync(Guid orgId, string inboundPath, Guid? excludeId, CancellationToken cancellationToken)
        => Task.FromResult(false);
}

internal sealed class FakeConverterAdminRepository : IConverterAdminRepository
{
    public Task<Guid> CreateAsync(AdminConverterCreateRequest request, CancellationToken cancellationToken)
        => Task.FromResult(Guid.NewGuid());

    public Task<bool> UpdateAsync(Guid orgId, Guid converterId, Dictionary<string, object?> updates, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> DeleteAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> ExistsAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

internal sealed class FakeConverterLogQueryRepository : IConverterLogQueryRepository
{
    public Task<IReadOnlyList<ConverterLogSummary>> GetRecentAsync(Guid converterId, int limit, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ConverterLogSummary>>(Array.Empty<ConverterLogSummary>());
}
