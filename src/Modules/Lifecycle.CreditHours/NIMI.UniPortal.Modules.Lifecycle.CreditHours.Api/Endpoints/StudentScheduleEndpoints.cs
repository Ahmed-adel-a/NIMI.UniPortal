using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Schedule;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Schedule.GetStudentSchedule;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>P4.5 — the student's own weekly schedule, built from their confirmed registrations.</summary>
internal static class StudentScheduleEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/schedule", GetSchedule)
            .WithTags("Lifecycle")
            .RequireAuthorization()
            .WithName("GetStudentSchedule");

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
}
