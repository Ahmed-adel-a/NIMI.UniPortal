using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Text;

namespace NIMI.UniPortal.Hosting.Lifecycle;

/// <summary>
/// Decision LIFECYCLEDEPT: both academic systems in every deployment (each department picks one), the router behind
/// <see cref="IAcademicLifecycle"/>, and the student routes both systems share.
/// </summary>
internal static class LifecycleEndpoints
{
    public static IServiceCollection AddLifecycle(this IServiceCollection services)
    {
        services.AddCreditHoursModule();
        services.AddSemesterBasedModule();
        services.AddScoped<IAcademicLifecycle, AcademicLifecycleRouter>();
        services.AddScoped<StudentLifecycleReads>();
        return services;
    }

    public static void MapLifecycleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCreditHoursEndpoints();
        endpoints.MapSemesterBasedEndpoints();

        endpoints.MapGet("/api/schedule", Schedule).WithTags("Lifecycle").RequireAuthorization().WithName("GetStudentSchedule");
        endpoints.MapGet("/api/schedule/export", ExportSchedule).WithTags("Lifecycle").RequireAuthorization().WithName("ExportStudentSchedule");
        endpoints.MapGet("/api/registration/progress", Progress).WithTags("Lifecycle").RequireAuthorization().WithName("GetStudentProgress");
        endpoints.MapGet("/api/teaching/groups/{groupId:guid}/roster", TeachingRoster)
            .WithTags("Lifecycle").RequireAuthorization().WithName("GetTeachingGroupRoster");
        endpoints.MapGet("/api/admin/groups/{groupId:guid}/roster", AdminRoster)
            .WithTags("Lifecycle").RequireAuthorization(PortalPolicies.Admin).WithName("GetAdminGroupRoster");
    }

    private static async Task<Ok<IReadOnlyList<StudentOccurrenceDto>>> Schedule(
        DateOnly? from, DateOnly? to, ClaimsPrincipal caller, StudentLifecycleReads reads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await reads.ScheduleAsync(caller.GetUserId(), caller.IsInRole(PortalRoles.Student), from, to, cancellationToken));

    /// <summary>P10.4: the same occurrences as CSV.</summary>
    private static async Task<FileContentHttpResult> ExportSchedule(
        DateOnly? from, DateOnly? to, ClaimsPrincipal caller, StudentLifecycleReads reads, CancellationToken cancellationToken)
    {
        var occurrences = await reads.ScheduleAsync(caller.GetUserId(), caller.IsInRole(PortalRoles.Student), from, to, cancellationToken);
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

    private static async Task<Ok<StudentProgressDto>> Progress(
        ClaimsPrincipal caller, StudentLifecycleReads reads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await reads.ProgressAsync(caller.GetUserId(), caller.IsInRole(PortalRoles.Student), cancellationToken));

    private static async Task<Ok<PagedResult<RosterStudentDto>>> TeachingRoster(
        Guid groupId, int? page, int? pageSize, ClaimsPrincipal caller, StudentLifecycleReads reads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await reads.RosterAsync(
            groupId, caller.GetUserId(), caller.IsTeachingStaff(), isAdmin: false, new PageRequest(page, pageSize), cancellationToken));

    private static async Task<Ok<PagedResult<RosterStudentDto>>> AdminRoster(
        Guid groupId, int? page, int? pageSize, StudentLifecycleReads reads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await reads.RosterAsync(
            groupId, Guid.Empty, isProfessor: false, isAdmin: true, new PageRequest(page, pageSize), cancellationToken));
}
