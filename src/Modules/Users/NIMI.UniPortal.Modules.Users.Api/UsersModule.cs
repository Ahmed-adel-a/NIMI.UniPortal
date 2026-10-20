using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Users.Api.Endpoints;
using NIMI.UniPortal.Modules.Users.Application;
using NIMI.UniPortal.Modules.Users.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Users.Api;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddUsersApplication();
        services.AddUsersInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        OrganizationEndpoints.Map(endpoints);
        return endpoints;
    }
}
