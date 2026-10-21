using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateGroup;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListGroups;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateGroup;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>P3.3 — the groups (شعب) a subject offers in a term (rule R4).</summary>
internal static class GroupEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var read = endpoints.MapGroup("/api/subjects/{subjectId:guid}/groups")
            .WithTags("Scheduling")
            .RequireAuthorization();

        read.MapGet("", ListGroups).WithName("ListGroups");

        var adminSubject = endpoints.MapGroup("/api/admin/subjects/{subjectId:guid}/groups")
            .WithTags("Scheduling")
            .RequireAuthorization(PortalPolicies.Admin);

        adminSubject.MapPost("", CreateGroup).WithName("CreateGroup");

        var admin = endpoints.MapGroup("/api/admin/groups")
            .WithTags("Scheduling")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapPut("/{id:guid}", UpdateGroup).WithName("UpdateGroup");
    }

    private static async Task<Ok<PagedResult<GroupDto>>> ListGroups(
        Guid subjectId,
        ISender sender,
        Guid? termId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListGroupsQuery(subjectId, termId, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Created<GroupDto>> CreateGroup(
        Guid subjectId,
        CreateGroupRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var group = await sender.Send(
            new CreateGroupCommand(
                subjectId,
                request.TermId,
                request.GroupNumber,
                request.TargetDepartmentId,
                request.Capacity),
            cancellationToken);
        return TypedResults.Created($"/api/subjects/{subjectId}/groups/{group.Id}", group);
    }

    private static async Task<Ok<GroupDto>> UpdateGroup(
        Guid id,
        UpdateGroupRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateGroupCommand(id, request.Capacity, request.TargetDepartmentId, request.IsActive),
            cancellationToken));
}
