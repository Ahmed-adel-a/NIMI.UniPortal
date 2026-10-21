using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateSubject;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.GetSubject;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListSubjects;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateSubject;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>P3.2 — the subject catalog (rule R3). Reading needs a signed-in user; managing is admin-only.</summary>
internal static class SubjectEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var read = endpoints.MapGroup("/api/subjects")
            .WithTags("Scheduling")
            .RequireAuthorization();

        read.MapGet("", ListSubjects).WithName("ListSubjects");
        read.MapGet("/{id:guid}", GetSubject).WithName("GetSubject");

        var admin = endpoints.MapGroup("/api/admin/subjects")
            .WithTags("Scheduling")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapPost("", CreateSubject).WithName("CreateSubject");
        admin.MapPut("/{id:guid}", UpdateSubject).WithName("UpdateSubject");
    }

    private static async Task<Ok<PagedResult<SubjectDto>>> ListSubjects(
        ISender sender,
        Guid? departmentId,
        string? search,
        bool? includeInactive,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListSubjectsQuery(
                departmentId,
                search,
                includeInactive ?? false,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Ok<SubjectDto>> GetSubject(
        Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetSubjectQuery(id), cancellationToken));

    private static async Task<Created<SubjectDto>> CreateSubject(
        CreateSubjectRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var subject = await sender.Send(
            new CreateSubjectCommand(
                request.DepartmentId,
                request.Code,
                request.Name,
                request.NameAr,
                request.Description,
                request.CreditHours,
                request.Category,
                request.IsGraduationRequirement),
            cancellationToken);
        return TypedResults.Created($"/api/subjects/{subject.Id}", subject);
    }

    private static async Task<Ok<SubjectDto>> UpdateSubject(
        Guid id, UpdateSubjectRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateSubjectCommand(
                id,
                request.Name,
                request.NameAr,
                request.Description,
                request.CreditHours,
                request.Category,
                request.IsGraduationRequirement,
                request.IsActive),
            cancellationToken));
}
