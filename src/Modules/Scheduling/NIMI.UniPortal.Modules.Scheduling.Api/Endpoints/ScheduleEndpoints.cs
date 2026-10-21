using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CancelScheduleSlot;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CreateScheduleSlot;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable.ListScheduleSlots;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable.UpdateScheduleSlot;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>P3.5 — timetable slots and their conflict detection (rules R6–R9).</summary>
internal static class ScheduleEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/schedules")
            .WithTags("Scheduling")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("", ListScheduleSlots).WithName("ListScheduleSlots");
        admin.MapPost("", CreateScheduleSlot).WithName("CreateScheduleSlot");
        admin.MapPut("/{id:guid}", UpdateScheduleSlot).WithName("UpdateScheduleSlot");
        admin.MapPost("/{id:guid}/cancel", CancelScheduleSlot).WithName("CancelScheduleSlot");
    }

    private static async Task<Ok<PagedResult<ScheduleSlotDto>>> ListScheduleSlots(
        ISender sender,
        Guid? termId,
        Guid? departmentId,
        Guid? facilityId,
        Guid? staffId,
        Guid? groupId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListScheduleSlotsQuery(
                termId,
                departmentId,
                facilityId,
                staffId,
                groupId,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Created<ScheduleSlotDto>> CreateScheduleSlot(
        CreateScheduleRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var slot = await sender.Send(
            new CreateScheduleSlotCommand(
                request.SubjectGroupId,
                request.FacilityId,
                request.StaffId,
                request.DayOfWeek,
                request.StartTime,
                request.EndTime,
                request.Recurrence,
                request.ValidFrom,
                request.ValidUntil,
                caller.GetUserId()),
            cancellationToken);
        return TypedResults.Created($"/api/admin/schedules/{slot.Id}", slot);
    }

    private static async Task<Ok<ScheduleSlotDto>> UpdateScheduleSlot(
        Guid id,
        UpdateScheduleRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateScheduleSlotCommand(
                id,
                request.SubjectGroupId,
                request.FacilityId,
                request.StaffId,
                request.DayOfWeek,
                request.StartTime,
                request.EndTime,
                request.Recurrence,
                request.ValidFrom,
                request.ValidUntil),
            cancellationToken));

    private static async Task<NoContent> CancelScheduleSlot(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelScheduleSlotCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
