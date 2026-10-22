using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

/// <summary>P4.8: Student Affairs generates each term's enrollments from the study plans and adjusts groups.</summary>
internal static class TermEnrollmentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/term-enrollments")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("/generate", Generate).WithName("GenerateTermEnrollments");
        admin.MapGet("", List).WithName("ListTermEnrollments");
        admin.MapPut("/{id:guid}/group", Move).WithName("MoveTermEnrollment");
    }

    private static async Task<Ok<GenerationReportDto>> Generate(
        GenerateEnrollmentRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GenerateTermEnrollmentCommand(request.TermId, request.DepartmentId), cancellationToken));

    private static async Task<Ok<PagedResult<TermEnrollmentDto>>> List(
        Guid? termId, Guid? studentId, Guid? groupId, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListTermEnrollmentsQuery(termId, studentId, groupId, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<TermEnrollmentDto>> Move(
        Guid id, MoveEnrollmentRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new MoveEnrollmentCommand(id, request.SubjectGroupId), cancellationToken));
}
