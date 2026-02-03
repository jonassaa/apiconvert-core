using Apiconvert.Api.Admin;
using Apiconvert.Api.Inbound;
using Apiconvert.Api.Organizations;
using Apiconvert.Api.Converters;
using Apiconvert.Api.Logs;
using Apiconvert.Core.Contracts;
using Apiconvert.Infrastructure.Ai;
using Apiconvert.Infrastructure.Auth;
using Apiconvert.Infrastructure.Data;
using Apiconvert.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Apiconvert.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddApiconvertInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ApiconvertDb")
            ?? configuration["APICONVERT_DB_CONNECTION"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "APICONVERT_DB_CONNECTION is not configured. Set it with 'dotnet user-secrets set \"APICONVERT_DB_CONNECTION\" \"<connection string>\"' or provide a connection string named 'ApiconvertDb'.");
        }

        var dataSource = NpgsqlDataSource.Create(connectionString);
        services.AddSingleton(dataSource);
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IConverterRepository, ConverterRepository>();
        services.AddScoped<IConverterQueryRepository, ConverterQueryRepository>();
        services.AddScoped<IMappingRepository, MappingRepository>();
        services.AddScoped<IConverterLogRepository, ConverterLogRepository>();
        services.AddScoped<IOrgMembershipRepository, OrgMembershipRepository>();
        services.AddScoped<IOrgRepository, OrgRepository>();
        services.AddScoped<IOrgSettingsRepository, OrgSettingsRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IOrgLogsRepository, OrgLogsRepository>();
        services.AddScoped<IConverterAdminRepository, ConverterAdminRepository>();
        services.AddScoped<IConverterLogQueryRepository, ConverterLogQueryRepository>();
        services.AddHttpClient<IForwarder, HttpForwarder>();
        var openRouterSiteUrl = configuration["OPENROUTER_SITE_URL"];
        var openRouterAppName = configuration["OPENROUTER_APP_NAME"];
        if (string.IsNullOrWhiteSpace(openRouterSiteUrl))
        {
            throw new InvalidOperationException("OPENROUTER_SITE_URL is not configured.");
        }
        if (string.IsNullOrWhiteSpace(openRouterAppName))
        {
            throw new InvalidOperationException("OPENROUTER_APP_NAME is not configured.");
        }

        services.AddHttpClient<OpenRouterConversionRulesGenerator>();
        services.AddHttpClient<SupabaseUserDirectory>();
        services.AddSingleton(new OpenRouterOptions
        {
            ApiKey = configuration["OPENROUTER_API_KEY"] ?? string.Empty,
            BaseUrl = configuration["OPENROUTER_BASE_URL"] ?? "https://openrouter.ai/api/v1",
            Model = configuration["OPENROUTER_MODEL"] ?? "openai/gpt-4o-mini",
            SiteUrl = openRouterSiteUrl,
            AppName = openRouterAppName
        });
        services.AddSingleton(new SupabaseAdminOptions
        {
            Url = configuration["SUPABASE_URL"] ?? configuration["NEXT_PUBLIC_SUPABASE_URL"] ?? string.Empty,
            ServiceRoleKey = configuration["SUPABASE_SECRET_KEY"] ?? string.Empty
        });
        services.AddScoped<IConversionRulesGenerator, OpenRouterConversionRulesGenerator>();
        services.AddScoped<IUserDirectory, SupabaseUserDirectory>();
        return services;
    }
}
