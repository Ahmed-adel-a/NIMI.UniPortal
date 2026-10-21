using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.GetMyPayments;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.GetReceipt;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>P5.4 — the student's payment history and printable receipts.</summary>
internal static class HistoryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var read = endpoints
            .MapGroup("/api/payments")
            .WithTags("Payments")
            .RequireAuthorization();

        read.MapGet("/history", History).WithName("GetPaymentHistory");
        read.MapGet("/{id:guid}/receipt", Receipt).WithName("GetPaymentReceipt");
    }

    private static async Task<Ok<PagedResult<PaymentHistoryItemDto>>> History(
        ClaimsPrincipal caller,
        ISender sender,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetMyPaymentsQuery(caller.GetUserId(), new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Ok<PaymentReceiptDto>> Receipt(
        Guid id,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetPaymentReceiptQuery(
                id,
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Admin)),
            cancellationToken));
}
