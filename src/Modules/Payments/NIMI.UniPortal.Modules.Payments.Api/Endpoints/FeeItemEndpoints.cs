using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Payments.Application.Fees;
using NIMI.UniPortal.Modules.Payments.Application.Fees.CreateFeeItem;
using NIMI.UniPortal.Modules.Payments.Application.Fees.ListFeeItems;
using NIMI.UniPortal.Modules.Payments.Application.Fees.UpdateFeeItem;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>P5.1 — Student Affairs manages the fee-item catalog (PAYOWN).</summary>
internal static class FeeItemEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints
            .MapGroup("/api/admin/fee-items")
            .WithTags("Payments")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("", List).WithName("ListFeeItems");
        admin.MapPost("", Create).WithName("CreateFeeItem");
        admin.MapPut("/{id:guid}", Update).WithName("UpdateFeeItem");
    }

    private static async Task<Ok<PagedResult<FeeItemDto>>> List(
        ISender sender,
        Guid? termId,
        string? category,
        bool? includeInactive,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListFeeItemsQuery(
                termId,
                category,
                includeInactive ?? false,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Created<FeeItemDto>> Create(
        CreateFeeItemRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var fee = await sender.Send(
            new CreateFeeItemCommand(
                request.Name,
                request.NameAr,
                request.Description,
                request.Category,
                request.Amount,
                request.TermId,
                request.FacultyId,
                request.DepartmentId,
                request.DueOn,
                caller.GetUserId()),
            cancellationToken);

        return TypedResults.Created($"/api/admin/fee-items/{fee.Id}", fee);
    }

    private static async Task<Ok<FeeItemDto>> Update(
        Guid id,
        UpdateFeeItemRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateFeeItemCommand(
                id,
                request.Name,
                request.NameAr,
                request.Description,
                request.Amount,
                request.DueOn,
                request.IsActive),
            cancellationToken));
}
