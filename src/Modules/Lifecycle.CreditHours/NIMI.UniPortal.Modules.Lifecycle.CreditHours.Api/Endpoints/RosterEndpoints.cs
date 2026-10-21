using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Rosters;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Rosters.GetGroupRoster;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>P4.4 — group rosters for the teaching professor (rule R5) and for admins, paged (rule R11).</summary>
internal static class RosterEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var teaching = endpoints.MapGroup("/api/teaching/groups/{groupId:guid}/roster")
            .WithTags("Lifecycle")
            .RequireAuthorization();

        teaching.MapGet("", GetTeaching).WithName("GetTeachingGroupRoster");

        var admin = endpoints.MapGroup("/api/admin/groups/{groupId:guid}/roster")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("", GetAdmin).WithName("GetAdminGroupRoster");
    }

    private static async Task<Ok<PagedResult<RosterStudentDto>>> GetTeaching(
        Guid groupId,
        ClaimsPrincipal caller,
        ISender sender,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetGroupRosterQuery(
                groupId,
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Professor),
                IsAdmin: false,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Ok<PagedResult<RosterStudentDto>>> GetAdmin(
        Guid groupId,
        ISender sender,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetGroupRosterQuery(
                groupId, Guid.Empty, IsProfessor: false, IsAdmin: true, new PageRequest(page, pageSize)),
            cancellationToken));
}
