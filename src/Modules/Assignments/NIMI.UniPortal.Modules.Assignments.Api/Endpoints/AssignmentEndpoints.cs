using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Assignments.Application;
using NIMI.UniPortal.Modules.Assignments.Application.Manage;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Api.Endpoints;

/// <summary>P8.1–P8.2: assignments set by a subject's teaching staff.</summary>
internal static class AssignmentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/assignments")
            .WithTags("Assignments")
            .RequireAuthorization();

        group.MapPost("/", Create).WithName("CreateAssignment");
        group.MapPut("/{id:guid}", Update).WithName("UpdateAssignment");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteAssignment");
    }

    private static async Task<Created<AssignmentDto>> Create(
        AssignmentRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var assignment = await sender.Send(
            new CreateAssignmentCommand(
                request.SubjectId,
                request.SubjectGroupId,
                request.Title ?? string.Empty,
                request.Instructions,
                request.DueAt,
                request.MaxGrade,
                request.AllowedExtensions ?? AssignmentRequest.DefaultExtensions,
                request.MaxFileMb ?? AssignmentRequest.DefaultMaxFileMb,
                CallerViewer.From(caller)),
            cancellationToken);
        return TypedResults.Created($"/api/assignments/{assignment.Id}", assignment);
    }

    private static async Task<Ok<AssignmentDto>> Update(
        Guid id, AssignmentRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateAssignmentCommand(
                id,
                request.Title ?? string.Empty,
                request.Instructions,
                request.DueAt,
                request.MaxGrade,
                request.AllowedExtensions ?? AssignmentRequest.DefaultExtensions,
                request.MaxFileMb ?? AssignmentRequest.DefaultMaxFileMb,
                CallerViewer.From(caller)),
            cancellationToken));

    private static async Task<NoContent> Delete(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAssignmentCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.NoContent();
    }
}
