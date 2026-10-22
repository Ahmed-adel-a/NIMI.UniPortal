using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Events.Api.Endpoints;
using NIMI.UniPortal.Modules.Events.Application;
using NIMI.UniPortal.Modules.Events.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Events.Api;

/// <summary>Premium module (P11): registered only by hosts that sell it — Cairo does.</summary>
public static class EventsModule
{
    public static IServiceCollection AddEventsModule(this IServiceCollection services)
    {
        services.AddEventsApplication();
        services.AddEventsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapEventsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        EventAdminEndpoints.Map(endpoints);
        EventMemberEndpoints.Map(endpoints);
        return endpoints;
    }
}
