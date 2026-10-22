using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Application.Posts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>Replies: post, edit, delete.</summary>
internal static class PostEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/forum/threads/{threadId:guid}/posts", Reply)
            .WithTags("Forum").RequireAuthorization().WithName("ReplyToForumThread");

        var posts = endpoints.MapGroup("/api/forum/posts/{id:guid}")
            .WithTags("Forum")
            .RequireAuthorization();
        posts.MapPut("", Edit).WithName("EditForumPost");
        posts.MapDelete("", Delete).WithName("DeleteForumPost");
    }

    private static async Task<Created<PostDto>> Reply(
        Guid threadId, PostRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var post = await sender.Send(new ReplyCommand(threadId, request.Body, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.Created($"/api/forum/threads/{threadId}", post);
    }

    private static async Task<Ok<PostDto>> Edit(
        Guid id, PostRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new EditPostCommand(id, request.Body, CallerViewer.From(caller)), cancellationToken));

    private static async Task<NoContent> Delete(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePostCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.NoContent();
    }
}
