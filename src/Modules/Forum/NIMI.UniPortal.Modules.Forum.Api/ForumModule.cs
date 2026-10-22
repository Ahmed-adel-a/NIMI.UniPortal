using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Forum.Api.Endpoints;
using NIMI.UniPortal.Modules.Forum.Application;
using NIMI.UniPortal.Modules.Forum.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Forum.Api;

/// <summary>Premium module (P11): registered only by hosts that sell it — Cairo does.</summary>
public static class ForumModule
{
    public static IServiceCollection AddForumModule(this IServiceCollection services)
    {
        services.AddForumApplication();
        services.AddForumInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapForumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        CategoryEndpoints.Map(endpoints);
        ThreadEndpoints.Map(endpoints);
        PostEndpoints.Map(endpoints);
        ReportEndpoints.Map(endpoints);
        return endpoints;
    }
}
