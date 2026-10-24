using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListMyClasses;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable.GetTeachingSchedule;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>P3.6 — a professor's own teaching occurrences (rules R9, R10). Returns a plain array, not a page.</summary>
internal static class TeachingScheduleEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/teaching/schedule", GetTeachingSchedule)
            .WithTags("Scheduling")
            .RequireAuthorization()
            .WithName("GetTeachingSchedule");
        endpoints.MapGet("/api/teaching/classes", GetMyClasses)
            .WithTags("Scheduling")
            .RequireAuthorization()
            .WithName("GetMyTeachingClasses");
    }

    private static async Task<Ok<IReadOnlyList<TeachingClassDto>>> GetMyClasses(
        ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListMyClassesQuery(caller.GetUserId(), caller.IsTeachingStaff()), cancellationToken));

    private static async Task<Ok<IReadOnlyList<TeachingOccurrenceDto>>> GetTeachingSchedule(
        DateOnly? from,
        DateOnly? to,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetTeachingScheduleQuery(
                caller.GetUserId(),
                caller.IsTeachingStaff(),
                from,
                to),
            cancellationToken));
}
