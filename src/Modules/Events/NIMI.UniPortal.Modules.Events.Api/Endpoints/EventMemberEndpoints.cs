using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Application.Participate;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Api.Endpoints;

/// <summary>Members browse events of their audience, register, cancel and see their own.</summary>
internal static class EventMemberEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var events = endpoints.MapGroup("/api/events")
            .WithTags("Events")
            .RequireAuthorization();
        events.MapGet("/", List).WithName("ListEvents");
        events.MapGet("/mine", Mine).WithName("MyEvents");
        events.MapGet("/{id:guid}", Get).WithName("GetEvent");
        events.MapPost("/{id:guid}/registrations", Register).WithName("RegisterForEvent");
        events.MapDelete("/{id:guid}/registrations/mine", CancelMine).WithName("CancelMyEventRegistration");
    }

    private static async Task<Ok<PagedResult<EventDto>>> List(
        DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListEventsQuery(CallerViewer.From(caller), from, to, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<PagedResult<EventDto>>> Mine(
        int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new MyEventsQuery(CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<EventDto>> Get(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetEventQuery(id, CallerViewer.From(caller)), cancellationToken));

    private static async Task<Created<RegistrationDto>> Register(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var registration = await sender.Send(new RegisterCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.Created($"/api/events/{id}", registration);
    }

    private static async Task<NoContent> CancelMine(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelRegistrationCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.NoContent();
    }
}
