using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Application.Manage;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Events.Api.Endpoints;

/// <summary>Student Affairs runs events: create, edit, publish, cancel, attendees, check-in.</summary>
internal static class EventAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/events")
            .WithTags("Events")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("/", Create).WithName("CreateEvent");
        admin.MapGet("/", List).WithName("ListAdminEvents");
        admin.MapPut("/{id:guid}", Update).WithName("UpdateEvent");
        admin.MapPost("/{id:guid}/publish", Publish).WithName("PublishEvent");
        admin.MapPost("/{id:guid}/cancel", Cancel).WithName("CancelEvent");
        admin.MapGet("/{id:guid}/registrations", Attendees).WithName("ListEventAttendees");
        admin.MapPost("/{id:guid}/check-in", CheckIn).WithName("CheckInToEvent");
    }

    private static async Task<Created<EventDto>> Create(EventRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var created = await sender.Send(Save(null, request, caller), cancellationToken);
        return TypedResults.Created($"/api/events/{created.Id}", created);
    }

    private static async Task<Ok<EventDto>> Update(
        Guid id, EventRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(Save(id, request, caller), cancellationToken));

    private static async Task<Ok<PagedResult<EventDto>>> List(
        string? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize,
        ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListAdminEventsQuery(status, from, to, caller.GetUserId(), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<EventDto>> Publish(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new PublishEventCommand(id, caller.GetUserId()), cancellationToken));

    private static async Task<Ok<EventDto>> Cancel(
        Guid id, CancelEventRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new CancelEventCommand(id, request.Reason, caller.GetUserId()), cancellationToken));

    private static async Task<Ok<PagedResult<AttendeeDto>>> Attendees(
        Guid id, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListAttendeesQuery(id, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<RegistrationDto>> CheckIn(
        Guid id, CheckInRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new CheckInCommand(id, request.UserId), cancellationToken));

    private static SaveEventCommand Save(Guid? id, EventRequest request, ClaimsPrincipal caller) =>
        new(id, request.Title, request.Description, request.FacultyId, request.Venue, request.StartsAt, request.EndsAt,
            request.Capacity, request.RegistrationDeadline, caller.GetUserId());
}
