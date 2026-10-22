using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Application.Threads;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>Threads: list per category, start, open (with replies), edit, delete, pin and lock.</summary>
internal static class ThreadEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/forum/categories/{categoryId:guid}/threads", List)
            .WithTags("Forum").RequireAuthorization().WithName("ListForumThreads");
        endpoints.MapPost("/api/forum/categories/{categoryId:guid}/threads", Start)
            .WithTags("Forum").RequireAuthorization().WithName("StartForumThread");

        var threads = endpoints.MapGroup("/api/forum/threads/{id:guid}")
            .WithTags("Forum")
            .RequireAuthorization();
        threads.MapGet("", View).WithName("GetForumThread");
        threads.MapPut("", Edit).WithName("EditForumThread");
        threads.MapDelete("", Delete).WithName("DeleteForumThread");
        threads.MapPost("/pin", Pin).WithName("PinForumThread");
        threads.MapPost("/lock", Lock).WithName("LockForumThread");
    }

    private static async Task<Ok<PagedResult<ThreadSummaryDto>>> List(
        Guid categoryId, int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListThreadsQuery(categoryId, CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Created<ThreadDto>> Start(
        Guid categoryId, ThreadRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var thread = await sender.Send(
            new StartThreadCommand(categoryId, request.Title, request.Body, request.SubjectId, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.Created($"/api/forum/threads/{thread.Id}", thread);
    }

    private static async Task<Ok<ThreadDto>> View(
        Guid id, int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ViewThreadCommand(id, CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<ThreadDto>> Edit(
        Guid id, ThreadRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new EditThreadCommand(id, request.Title, request.Body, CallerViewer.From(caller)), cancellationToken));

    private static async Task<NoContent> Delete(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteThreadCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<ThreadDto>> Pin(
        Guid id, PinRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ModerateThreadCommand(id, request.Pinned, null, CallerViewer.From(caller)), cancellationToken));

    private static async Task<Ok<ThreadDto>> Lock(
        Guid id, LockRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ModerateThreadCommand(id, null, request.Locked, CallerViewer.From(caller)), cancellationToken));
}
