using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

/// <summary>Decision SEMPROMO: the promotion settings, university-wide and per faculty (the faculty's override wins).</summary>
internal static class PromotionPolicyEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var university = endpoints.MapGroup("/api/admin/promotion-policy")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);
        university.MapGet("", GetUniversity).WithName("GetPromotionPolicy");
        university.MapPut("", SaveUniversity).WithName("SavePromotionPolicy");

        var faculty = endpoints.MapGroup("/api/admin/faculties/{facultyId:guid}/promotion-policy")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);
        faculty.MapGet("", GetFaculty).WithName("GetFacultyPromotionPolicy");
        faculty.MapPut("", SaveFaculty).WithName("SaveFacultyPromotionPolicy");
        faculty.MapDelete("", DeleteFaculty).WithName("DeleteFacultyPromotionPolicy");
    }

    private static async Task<Ok<PromotionPolicyDto>> GetUniversity(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetPromotionPolicyQuery(null), cancellationToken));

    private static async Task<Ok<PromotionPolicyDto>> SaveUniversity(
        SavePromotionPolicyRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(Command(null, request), cancellationToken));

    private static async Task<Ok<PromotionPolicyDto>> GetFaculty(Guid facultyId, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetPromotionPolicyQuery(facultyId), cancellationToken));

    private static async Task<Ok<PromotionPolicyDto>> SaveFaculty(
        Guid facultyId, SavePromotionPolicyRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(Command(facultyId, request), cancellationToken));

    private static async Task<NoContent> DeleteFaculty(Guid facultyId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteFacultyPromotionPolicyCommand(facultyId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static SavePromotionPolicyCommand Command(Guid? facultyId, SavePromotionPolicyRequest request) =>
        new(facultyId, request.MaxCarriedSubjects, request.GateLevels, request.MaxYearsPerLevel, request.RequireFeesPaid);
}
