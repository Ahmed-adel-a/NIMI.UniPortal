using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Attendance.Application.Devices;
using NIMI.UniPortal.Modules.Attendance.Application.Devices.Decide;
using NIMI.UniPortal.Modules.Attendance.Application.Devices.List;
using NIMI.UniPortal.Modules.Attendance.Application.Devices.RequestChange;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>P6.8: students request a new attendance phone; Student Affairs decides (DEVICE, R11).</summary>
internal static class DeviceChangeEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/attendance/device-change", RequestChange)
            .WithTags("Attendance")
            .RequireAuthorization()
            .WithName("RequestDeviceChange");

        var admin = endpoints.MapGroup("/api/admin/attendance/device-changes")
            .WithTags("Attendance")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("/", List).WithName("ListDeviceChanges");
        admin.MapPost("/{id:guid}/approve", Approve).WithName("ApproveDeviceChange");
        admin.MapPost("/{id:guid}/reject", Reject).WithName("RejectDeviceChange");
    }

    private static async Task<Created<DeviceChangeDto>> RequestChange(
        DeviceChangeBody body,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var request = await sender.Send(
            new RequestDeviceChangeCommand(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Student),
                body.NewFingerprint ?? string.Empty,
                body.Reason ?? string.Empty),
            cancellationToken);
        return TypedResults.Created($"/api/admin/attendance/device-changes/{request.Id}", request);
    }

    private static async Task<Ok<PagedResult<DeviceChangeItemDto>>> List(
        string? status,
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListDeviceChangesQuery(status, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<DeviceChangeDto>> Approve(
        Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new DecideDeviceChangeCommand(id, true, caller.GetUserId()), cancellationToken));

    private static async Task<Ok<DeviceChangeDto>> Reject(
        Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new DecideDeviceChangeCommand(id, false, caller.GetUserId()), cancellationToken));
}
