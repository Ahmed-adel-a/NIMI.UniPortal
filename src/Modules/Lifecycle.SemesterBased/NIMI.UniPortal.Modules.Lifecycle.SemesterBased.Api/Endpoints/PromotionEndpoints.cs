using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

/// <summary>P4.9: year-end decisions — history, an explicit re-run and Student Affairs overrides (decision SEMPROMO).</summary>
internal static class PromotionEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/promotions")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapGet("", List).WithName("ListPromotions");
        admin.MapPost("/run", Run).WithName("RunPromotions");
        admin.MapPost("/override", Override).WithName("OverridePromotion");
    }

    private static async Task<Ok<PagedResult<PromotionDto>>> List(
        int? academicYear, Guid? departmentId, Guid? studentId, PromotionResult? result, int? page, int? pageSize,
        ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListPromotionsQuery(academicYear, departmentId, studentId, result, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<RunPromotionsDto>> Run(
        RunPromotionsRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new RunPromotionsCommand(request.TermId), cancellationToken));

    private static async Task<Ok<PromotionDto>> Override(
        OverridePromotionRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new OverridePromotionCommand(request.StudentId, request.Result, request.Reason, caller.GetUserId()), cancellationToken));
}
