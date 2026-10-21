using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>P9.4: graduation requirements per department (GRADREQ) and the eligibility list — Student Affairs only.</summary>
internal static class GraduationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/graduation")
            .WithTags("Graduation")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapGet("/requirements/{departmentId:guid}", GetRequirement).WithName("GetGraduationRequirement");
        admin.MapPut("/requirements/{departmentId:guid}", SetRequirement).WithName("SetGraduationRequirement");
        admin.MapGet("/eligibility", Eligibility).WithName("GraduationEligibility");
    }

    private static async Task<Ok<GraduationRequirementDto>> GetRequirement(
        Guid departmentId, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetGraduationRequirementQuery(departmentId), cancellationToken));

    private static async Task<Ok<GraduationRequirementDto>> SetRequirement(
        Guid departmentId, SetGraduationRequirementRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new SetGraduationRequirementCommand(departmentId, request.RequiredCredits, caller.GetUserId()), cancellationToken));

    private static async Task<Ok<PagedResult<GraduationEligibilityDto>>> Eligibility(
        Guid? facultyId, Guid? departmentId, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GraduationEligibilityQuery(facultyId, departmentId, new PageRequest(page, pageSize)), cancellationToken));
}
