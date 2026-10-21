using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Announcements.Api.Endpoints;
using NIMI.UniPortal.Modules.Announcements.Application;
using NIMI.UniPortal.Modules.Announcements.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Announcements.Api;

public static class AnnouncementsModule
{
    public static IServiceCollection AddAnnouncementsModule(this IServiceCollection services)
    {
        services.AddAnnouncementsApplication();
        services.AddAnnouncementsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapAnnouncementsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        AnnouncementEndpoints.Map(endpoints);
        return endpoints;
    }
}
