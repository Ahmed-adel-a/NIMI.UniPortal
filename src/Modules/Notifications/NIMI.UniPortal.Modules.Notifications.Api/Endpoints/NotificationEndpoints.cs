using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Notifications.Application.Notifications;
using NIMI.UniPortal.Modules.Notifications.Application.Notifications.ListMine;
using NIMI.UniPortal.Modules.Notifications.Application.Notifications.MarkAllRead;
using NIMI.UniPortal.Modules.Notifications.Application.Notifications.MarkRead;
using NIMI.UniPortal.Modules.Notifications.Application.Notifications.UnreadCount;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Notifications.Api.Endpoints;

/// <summary>
/// P2.2 — the signed-in user's own notifications (default policy: authenticated + password changed).
/// Every handler filters by the caller's id; an unknown or foreign id is a 404 (rule R4).
/// </summary>
internal static class NotificationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var mine = endpoints.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        mine.MapGet("/mine", ListMine).WithName("ListMyNotifications");
        mine.MapGet("/mine/unread-count", GetUnreadCount).WithName("GetUnreadNotificationCount");
        mine.MapPost("/{id:guid}/read", MarkRead).WithName("MarkNotificationRead");
        mine.MapPost("/read-all", MarkAllRead).WithName("MarkAllNotificationsRead");
    }

    private static async Task<Ok<PagedResult<NotificationDto>>> ListMine(
        ClaimsPrincipal caller, bool? unreadOnly, int? page, int? pageSize,
        ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListMyNotificationsQuery(caller.GetUserId(), unreadOnly ?? false, new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Ok<UnreadCountDto>> GetUnreadCount(
        ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetUnreadCountQuery(caller.GetUserId()), cancellationToken));

    private static async Task<NoContent> MarkRead(
        Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNotificationReadCommand(caller.GetUserId(), id), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkAllRead(
        ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkAllNotificationsReadCommand(caller.GetUserId()), cancellationToken);
        return TypedResults.NoContent();
    }
}
