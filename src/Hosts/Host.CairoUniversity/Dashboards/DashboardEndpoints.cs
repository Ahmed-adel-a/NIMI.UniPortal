using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Security;

namespace Host.CairoUniversity.Dashboards;

/// <summary>P10: dashboards composed in the host from module contracts.</summary>
internal static class DashboardEndpoints
{
    public static IServiceCollection AddDashboards(this IServiceCollection services)
    {
        services.AddScoped<StudentDashboardComposer>();
        services.AddScoped<StudentAffairsDashboardComposer>();
        return services;
    }

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", StudentDashboard)
            .WithTags("Dashboards")
            .RequireAuthorization()
            .WithName("StudentDashboard");

        var admin = endpoints.MapGroup("/api/admin/dashboard")
            .WithTags("Dashboards")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapGet("/metrics", Metrics).WithName("DashboardMetrics");
        admin.MapGet("/summary", Summary).WithName("DashboardSummary");
        return endpoints;
    }

    private static async Task<Ok<StudentDashboardDto>> StudentDashboard(
        ClaimsPrincipal caller, StudentDashboardComposer composer, CancellationToken cancellationToken)
    {
        if (!caller.IsInRole(PortalRoles.Student))
            throw new ForbiddenException("The dashboard is for students.");

        return TypedResults.Ok(await composer.ComposeAsync(caller.GetUserId(), cancellationToken));
    }

    private static Ok<IReadOnlyList<DashboardMetricDto>> Metrics() => TypedResults.Ok(DashboardMetrics.Catalog);

    private static async Task<Ok<StudentAffairsSummaryDto>> Summary(
        Guid? termId, Guid? facultyId, Guid? departmentId, StudentAffairsDashboardComposer composer, CancellationToken cancellationToken) =>
        TypedResults.Ok(await composer.ComposeAsync(termId, facultyId, departmentId, cancellationToken));
}
