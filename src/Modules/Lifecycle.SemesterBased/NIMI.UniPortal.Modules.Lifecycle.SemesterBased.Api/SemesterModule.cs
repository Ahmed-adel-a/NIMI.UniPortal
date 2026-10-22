using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api;

/// <summary>
/// Decision LIFECYCLEDEPT: the year-group (نظام الفرق) variant, installed in every deployment next to the credit-hours one.
/// The shared student routes (schedule, rosters, progress) live in the host and ask the lifecycle router.
/// </summary>
public static class SemesterModule
{
    public static IServiceCollection AddSemesterBasedModule(this IServiceCollection services)
    {
        services.AddSemesterApplication();
        services.AddSemesterInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapSemesterBasedEndpoints(this IEndpointRouteBuilder endpoints)
    {
        StudyPlanEndpoints.Map(endpoints);
        PromotionPolicyEndpoints.Map(endpoints);
        StandingEndpoints.Map(endpoints);
        TermEnrollmentEndpoints.Map(endpoints);
        PromotionEndpoints.Map(endpoints);
        return endpoints;
    }
}
