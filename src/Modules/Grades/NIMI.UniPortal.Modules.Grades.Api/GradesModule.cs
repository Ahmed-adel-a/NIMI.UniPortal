using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Grades.Api.Endpoints;
using NIMI.UniPortal.Modules.Grades.Application;
using NIMI.UniPortal.Modules.Grades.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Grades.Api;

public static class GradesModule
{
    public static IServiceCollection AddGradesModule(this IServiceCollection services)
    {
        services.AddGradesApplication();
        services.AddGradesInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapGradesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        GroupGradeEndpoints.Map(endpoints);
        GradeCsvEndpoints.Map(endpoints);
        PublishingEndpoints.Map(endpoints);
        MyGradesEndpoints.Map(endpoints);
        CorrectionEndpoints.Map(endpoints);
        return endpoints;
    }
}
