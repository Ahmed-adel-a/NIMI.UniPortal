using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Domain.Online;

namespace NIMI.UniPortal.Modules.Payments.Application.Online;

/// <summary>The return page and mock-complete response: status as of now plus receipt when succeeded (P5.7).</summary>
public sealed record PaymentIntentDto(
    Guid Id,
    string Status,
    decimal Amount,
    string? ReceiptNumber,
    string? FailureReason)
{
    public static PaymentIntentDto From(
        PaymentIntent intent,
        PaymentIntentStatus status,
        string? receiptNumber) =>
        new(
            intent.Id,
            EnumWire.ToWire(status),
            intent.Amount,
            status == PaymentIntentStatus.Succeeded ? receiptNumber : null,
            intent.FailureReason);
}
