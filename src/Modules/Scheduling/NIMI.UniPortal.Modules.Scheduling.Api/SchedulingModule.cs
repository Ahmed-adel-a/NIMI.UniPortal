using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;
using NIMI.UniPortal.Modules.Scheduling.Application;
using NIMI.UniPortal.Modules.Scheduling.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Scheduling.Api;

public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services)
    {
        services.AddSchedulingApplication();
        services.AddSchedulingInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        TermEndpoints.Map(endpoints);
        SubjectEndpoints.Map(endpoints);
        GroupEndpoints.Map(endpoints);
        TeachingAssignmentEndpoints.Map(endpoints);
        FacilityEndpoints.Map(endpoints);
        ScheduleEndpoints.Map(endpoints);
        return endpoints;
    }
}
