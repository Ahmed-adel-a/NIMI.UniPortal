using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Assignments.Api.Endpoints;
using NIMI.UniPortal.Modules.Assignments.Application;
using NIMI.UniPortal.Modules.Assignments.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Assignments.Api;

public static class AssignmentsModule
{
    public static IServiceCollection AddAssignmentsModule(this IServiceCollection services)
    {
        services.AddAssignmentsApplication();
        services.AddAssignmentsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapAssignmentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        AssignmentEndpoints.Map(endpoints);
        SubmissionEndpoints.Map(endpoints);
        return endpoints;
    }
}
