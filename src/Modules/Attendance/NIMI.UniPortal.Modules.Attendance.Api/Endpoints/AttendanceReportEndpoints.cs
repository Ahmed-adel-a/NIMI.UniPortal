using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.GroupSummary;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.History;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>P6.6–P6.7: attendance history, group summaries and the at-risk list.</summary>
internal static class AttendanceReportEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/attendance")
            .WithTags("Attendance")
            .RequireAuthorization();

        group.MapGet("/history", GetHistory).WithName("GetAttendanceHistory");
        group.MapGet("/groups/{groupId:guid}/summary", GetGroupSummary).WithName("GetAttendanceGroupSummary");
    }

    private static async Task<Ok<AttendanceHistoryDto>> GetHistory(
        Guid? subjectId,
        Guid? termId,
        int? page,
        int? pageSize,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetAttendanceHistoryQuery(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Student),
                subjectId,
                termId,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Ok<PagedResult<GroupSummaryItemDto>>> GetGroupSummary(
        Guid groupId,
        int? page,
        int? pageSize,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetGroupSummaryQuery(groupId, caller.GetUserId(), caller.IsInRole(PortalRoles.Admin), new PageRequest(page, pageSize)),
            cancellationToken));
}
