using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.GetPaymentReport;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.ListPayments;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.RecordPayment;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>P5.3/P5.4/P5.6 — Student Affairs records, lists and reports payments.</summary>
internal static class OfficePaymentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints
            .MapGroup("/api/admin/payments")
            .WithTags("Payments")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapPost("", Record).WithName("RecordPayment");
        admin.MapGet("", List).WithName("ListPayments");
        admin.MapGet("/report", Report).WithName("GetPaymentReport");
    }

    private static async Task<Created<PaymentRecordDto>> Record(
        RecordPaymentRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var payment = await sender.Send(
            new RecordPaymentCommand(
                request.StudentId,
                request.FeeItemId,
                request.Amount,
                request.Method,
                request.Reference,
                request.PaidAt,
                caller.GetUserId()),
            cancellationToken);

        return TypedResults.Created($"/api/payments/{payment.Id}/receipt", payment);
    }

    private static async Task<Ok<PagedResult<AdminPaymentDto>>> List(
        ISender sender,
        Guid? studentId,
        Guid? feeItemId,
        DateOnly? from,
        DateOnly? to,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListPaymentsQuery(
                studentId,
                feeItemId,
                from,
                to,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Ok<PaymentReportDto>> Report(
        ISender sender,
        Guid? termId,
        Guid? facultyId,
        Guid? departmentId,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetPaymentReportQuery(termId, facultyId, departmentId),
            cancellationToken));
}
