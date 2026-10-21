using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Schedule;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Schedule.GetStudentSchedule;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Text;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>P4.5 — the student's own weekly schedule, built from their confirmed registrations; P10.4 — the same as CSV.</summary>
internal static class StudentScheduleEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/schedule", GetSchedule)
            .WithTags("Lifecycle")
            .RequireAuthorization()
            .WithName("GetStudentSchedule");
        endpoints.MapGet("/api/schedule/export", ExportSchedule)
            .WithTags("Lifecycle")
            .RequireAuthorization()
            .WithName("ExportStudentSchedule");
    }

    private static async Task<Ok<IReadOnlyList<StudentOccurrenceDto>>> GetSchedule(
        ClaimsPrincipal caller,
        ISender sender,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetStudentScheduleQuery(
                caller.GetUserId(), caller.IsInRole(PortalRoles.Student), from, to),
            cancellationToken));

    private static async Task<FileContentHttpResult> ExportSchedule(
        ClaimsPrincipal caller,
        ISender sender,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var occurrences = await sender.Send(
            new GetStudentScheduleQuery(caller.GetUserId(), caller.IsInRole(PortalRoles.Student), from, to),
            cancellationToken);

        var csv = new Csv().Row("Date", "Day", "Start", "End", "Subject code", "Subject", "Subject (Arabic)", "Group", "Hall");
        foreach (var o in occurrences)
        {
            csv.Row(
                o.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                o.DayOfWeek.ToString(CultureInfo.InvariantCulture),
                o.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                o.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                o.SubjectCode,
                o.SubjectName,
                o.SubjectNameAr,
                o.GroupNumber.ToString(CultureInfo.InvariantCulture),
                o.FacilityName);
        }

        var fileName = string.Create(CultureInfo.InvariantCulture, $"schedule-{from:yyyy-MM-dd}-{to:yyyy-MM-dd}.csv");
        return TypedResults.File(csv.ToUtf8WithBom(), Csv.ContentType, fileName);
    }
}
