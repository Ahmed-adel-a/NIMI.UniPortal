using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Attendance.Application.Sessions;
using NIMI.UniPortal.Modules.Attendance.Application.Sessions.GetSessionQr;
using NIMI.UniPortal.Modules.Attendance.Application.Sessions.OpenSession;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>P6.1: the professor opens a lecture session and shows its rotating QR code.</summary>
internal static class AttendanceSessionEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/attendance")
            .WithTags("Attendance")
            .RequireAuthorization();

        group.MapPost("/sessions", OpenSession).WithName("OpenAttendanceSession");
        group.MapGet("/sessions/{id:guid}/qr", GetSessionQr).WithName("GetAttendanceSessionQr");
    }

    private static async Task<Created<AttendanceSessionDto>> OpenSession(
        OpenAttendanceSessionRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var session = await sender.Send(
            new OpenSessionCommand(request.SlotId, request.Date, caller.GetUserId()),
            cancellationToken);
        return TypedResults.Created($"/api/attendance/sessions/{session.Id}", session);
    }

    private static async Task<Ok<SessionQrDto>> GetSessionQr(
        Guid id,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetSessionQrQuery(id, caller.GetUserId()), cancellationToken));
}
