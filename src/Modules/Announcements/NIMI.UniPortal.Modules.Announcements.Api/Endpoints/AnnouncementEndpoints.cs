using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Announcements.Application;
using NIMI.UniPortal.Modules.Announcements.Application.Browse;
using NIMI.UniPortal.Modules.Announcements.Application.Manage;
using NIMI.UniPortal.Modules.Announcements.Application.Post;
using NIMI.UniPortal.Modules.Announcements.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Announcements.Api.Endpoints;

/// <summary>P7.4–P7.5: subject announcements by teaching staff (ANN) and the student feed.</summary>
internal static class AnnouncementEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/announcements")
            .WithTags("Announcements")
            .RequireAuthorization();

        group.MapPost("/", Post).WithName("PostAnnouncement");
        group.MapPut("/{id:guid}", Edit).WithName("EditAnnouncement");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteAnnouncement");
        group.MapGet("/", List).WithName("ListAnnouncements");
        group.MapGet("/feed", Feed).WithName("GetAnnouncementFeed");
    }

    private static async Task<Ok<PagedResult<AnnouncementDto>>> List(
        Guid? subjectId, int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListAnnouncementsQuery(subjectId, CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<PagedResult<AnnouncementDto>>> Feed(
        int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetFeedQuery(CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Created<AnnouncementDto>> Post(
        AnnouncementRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var announcement = await sender.Send(
            new PostAnnouncementCommand(
                request.SubjectId,
                request.SubjectGroupId,
                request.Title ?? string.Empty,
                request.Body ?? string.Empty,
                request.Priority ?? AnnouncementPriority.Normal,
                request.IsPinned,
                request.ExpiresAt,
                CallerViewer.From(caller)),
            cancellationToken);
        return TypedResults.Created($"/api/announcements/{announcement.Id}", announcement);
    }

    private static async Task<Ok<AnnouncementDto>> Edit(
        Guid id, AnnouncementRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new EditAnnouncementCommand(
                id,
                request.Title ?? string.Empty,
                request.Body ?? string.Empty,
                request.Priority ?? AnnouncementPriority.Normal,
                request.IsPinned,
                request.ExpiresAt,
                CallerViewer.From(caller)),
            cancellationToken));

    private static async Task<NoContent> Delete(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAnnouncementCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.NoContent();
    }
}
