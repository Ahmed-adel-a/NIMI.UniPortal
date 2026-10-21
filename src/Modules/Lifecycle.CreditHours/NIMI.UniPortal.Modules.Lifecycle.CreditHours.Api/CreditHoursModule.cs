using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api;

/// <summary>Decision LIFECYCLE: the CreditHours variant composed by Host.CairoUniversity (one variant per host).</summary>
public static class CreditHoursModule
{
    public static IServiceCollection AddCreditHoursModule(this IServiceCollection services)
    {
        services.AddCreditHoursApplication();
        services.AddCreditHoursInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapCreditHoursEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RegistrationWindowEndpoints.Map(endpoints);
        PrerequisiteEndpoints.Map(endpoints);
        RegistrationEndpoints.Map(endpoints);
        return endpoints;
    }
}
