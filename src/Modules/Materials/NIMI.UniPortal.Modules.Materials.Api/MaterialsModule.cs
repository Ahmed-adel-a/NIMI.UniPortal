using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Materials.Api.Endpoints;
using NIMI.UniPortal.Modules.Materials.Application;
using NIMI.UniPortal.Modules.Materials.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Materials.Api;

public static class MaterialsModule
{
    public static IServiceCollection AddMaterialsModule(this IServiceCollection services)
    {
        services.AddMaterialsApplication();
        services.AddMaterialsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapMaterialsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MaterialEndpoints.Map(endpoints);
        return endpoints;
    }
}
