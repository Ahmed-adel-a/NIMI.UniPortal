using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

/// <summary>P4.7: Student Affairs keeps each department's study plan (levels and the subjects of each level and term).</summary>
internal static class StudyPlanEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/study-plans/{departmentId:guid}")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("", Get).WithName("GetStudyPlan");
        admin.MapPut("", Save).WithName("SaveStudyPlan");
    }

    private static async Task<Ok<StudyPlanDto>> Get(Guid departmentId, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetStudyPlanQuery(departmentId), cancellationToken));

    private static async Task<Ok<StudyPlanDto>> Save(
        Guid departmentId, SaveStudyPlanRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new SaveStudyPlanCommand(departmentId, request.Levels, request.Subjects), cancellationToken));
}
