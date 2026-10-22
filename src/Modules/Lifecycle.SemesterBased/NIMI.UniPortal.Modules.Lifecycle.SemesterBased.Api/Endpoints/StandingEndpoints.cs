using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

/// <summary>P4.7: year levels (الفرق) — Student Affairs places students and sees each department's cohorts; students see their own.</summary>
internal static class StandingEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapGet("/standings", List).WithName("ListStandings");
        admin.MapPut("/standings/{studentId:guid}", Place).WithName("PlaceStudent");
        admin.MapGet("/cohorts", Cohorts).WithName("GetCohorts");

        endpoints.MapGet("/api/standing", Mine)
            .WithTags("Lifecycle")
            .RequireAuthorization()
            .WithName("GetMyStanding");
    }

    private static async Task<Ok<PagedResult<StandingDto>>> List(
        Guid? departmentId, int? level, StandingStatus? status, int? page, int? pageSize, ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListStandingsQuery(departmentId, level, status, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<StandingDto>> Place(
        Guid studentId, PlaceStudentRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new PlaceStudentCommand(studentId, request.Level), cancellationToken));

    private static async Task<Ok<IReadOnlyList<CohortLevelDto>>> Cohorts(
        Guid departmentId, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetCohortsQuery(departmentId), cancellationToken));

    private static async Task<Ok<MyStandingDto>> Mine(ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetMyStandingQuery(caller.GetUserId(), caller.IsInRole(PortalRoles.Student)), cancellationToken));
}
