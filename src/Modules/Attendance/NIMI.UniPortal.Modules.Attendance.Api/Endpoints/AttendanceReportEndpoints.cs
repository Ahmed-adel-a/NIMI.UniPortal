using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.AtRisk;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.Eligibility;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.Export;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.GroupSummary;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.History;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Text;

namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>P6.6–P6.7: attendance history, group summaries and the at-risk list; P10.4–P10.5: CSV sheet and finals eligibility.</summary>
internal static class AttendanceReportEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/attendance")
            .WithTags("Attendance")
            .RequireAuthorization();

        group.MapGet("/history", GetHistory).WithName("GetAttendanceHistory");
        group.MapGet("/groups/{groupId:guid}/summary", GetGroupSummary).WithName("GetAttendanceGroupSummary");
        group.MapGet("/at-risk", GetAtRisk).WithName("GetAttendanceAtRisk");
        group.MapGet("/groups/{groupId:guid}/export", ExportGroup).WithName("ExportGroupAttendance");
        group.MapGet("/eligibility", GetEligibility).WithName("GetFinalsEligibility");
    }

    private static async Task<Ok<PagedResult<FinalsEligibilityItemDto>>> GetEligibility(
        Guid? termId,
        Guid? groupId,
        Guid? facultyId,
        bool? barredOnly,
        int? page,
        int? pageSize,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetFinalsEligibilityQuery(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Admin),
                caller.IsTeachingStaff(),
                termId,
                groupId,
                facultyId,
                barredOnly ?? false,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<FileContentHttpResult> ExportGroup(
        Guid groupId, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var sheet = await sender.Send(
            new GetAttendanceSheetQuery(groupId, caller.GetUserId(), caller.IsInRole(PortalRoles.Admin)), cancellationToken);

        var csv = new Csv().Row(
        [
            "Student code", "Name", "Attended", "Held", "Percentage",
            .. sheet.SessionDates.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ]);
        foreach (var row in sheet.Rows)
        {
            csv.Row(
            [
                row.StudentCode,
                row.FullName,
                row.Attended.ToString(CultureInfo.InvariantCulture),
                row.Held.ToString(CultureInfo.InvariantCulture),
                row.Percentage.ToString("0.00", CultureInfo.InvariantCulture),
                .. row.Statuses,
            ]);
        }

        var fileName = $"attendance-{sheet.SubjectCode}-group-{sheet.GroupNumber.ToString(CultureInfo.InvariantCulture)}.csv";
        return TypedResults.File(csv.ToUtf8WithBom(), Csv.ContentType, fileName);
    }

    private static async Task<Ok<PagedResult<AtRiskItemDto>>> GetAtRisk(
        Guid? termId,
        Guid? groupId,
        int? page,
        int? pageSize,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetAtRiskQuery(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Admin),
                caller.IsTeachingStaff(),
                termId,
                groupId,
                new PageRequest(page, pageSize)),
            cancellationToken));

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
