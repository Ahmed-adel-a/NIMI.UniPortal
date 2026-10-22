using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Library.Api.Endpoints;
using NIMI.UniPortal.Modules.Library.Application;
using NIMI.UniPortal.Modules.Library.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Library.Api;

/// <summary>Premium module (P11): registered only by hosts that sell it — the reference host does, Cairo does not.</summary>
public static class LibraryModule
{
    public static IServiceCollection AddLibraryModule(this IServiceCollection services)
    {
        services.AddLibraryApplication();
        services.AddLibraryInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        LibraryAdminEndpoints.Map(endpoints);
        LibraryMemberEndpoints.Map(endpoints);
        DigitalLibraryEndpoints.Map(endpoints);
        return endpoints;
    }
}
