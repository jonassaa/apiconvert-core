using Apiconvert.Api.Admin;
using Apiconvert.Api.Converters;
using Apiconvert.Api.Dashboard;
using Apiconvert.Api.Logs;
using Apiconvert.Api.Organizations;
using Microsoft.Extensions.DependencyInjection;

namespace Apiconvert.Api.Inbound;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddApiconvertApi(this IServiceCollection services)
    {
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        services.AddScoped<InboundHandler>();
        services.AddScoped<ConverterAdminService>();
        services.AddScoped<OrgService>();
        services.AddScoped<OrgSettingsService>();
        services.AddScoped<InviteService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ConverterService>();
        services.AddScoped<OrgLogsService>();
        return services;
    }
}
