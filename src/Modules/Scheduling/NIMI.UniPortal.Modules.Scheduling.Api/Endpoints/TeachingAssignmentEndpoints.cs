using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateTeachingAssignment;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.DeleteTeachingAssignment;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListTeachingAssignments;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>P3.3 — who teaches what (decision MULTIPROF, rule R5).</summary>
internal static class TeachingAssignmentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/teaching-assignments")
            .WithTags("Scheduling")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("", ListTeachingAssignments).WithName("ListTeachingAssignments");
        admin.MapPost("", CreateTeachingAssignment).WithName("CreateTeachingAssignment");
        admin.MapDelete("/{id:guid}", DeleteTeachingAssignment).WithName("DeleteTeachingAssignment");
    }

    private static async Task<Ok<PagedResult<TeachingAssignmentDto>>> ListTeachingAssignments(
        ISender sender,
        Guid? termId,
        Guid? staffId,
        Guid? subjectId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListTeachingAssignmentsQuery(termId, staffId, subjectId, new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Created<TeachingAssignmentDto>> CreateTeachingAssignment(
        CreateTeachingAssignmentRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var assignment = await sender.Send(
            new CreateTeachingAssignmentCommand(
                request.StaffId,
                request.SubjectId,
                request.TermId,
                request.SubjectGroupId,
                request.Role,
                caller.GetUserId()),
            cancellationToken);
        return TypedResults.Created($"/api/admin/teaching-assignments/{assignment.Id}", assignment);
    }

    private static async Task<NoContent> DeleteTeachingAssignment(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTeachingAssignmentCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
