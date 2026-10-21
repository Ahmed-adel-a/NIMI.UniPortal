using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Online.HandleCallback;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Payments.Application.Online;

internal sealed class MockPaymentCompleter(
    IPaymentsDbContext db,
    PaymentsGatewaySettings settings,
    ISender sender) : IMockPaymentCompleter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PaymentIntentDto> CompleteAsync(
        Guid intentId,
        Guid studentId,
        string outcome,
        CancellationToken cancellationToken)
    {
        var normalized = outcome?.Trim().ToLowerInvariant();
        if (normalized is not ("success" or "fail"))
            throw ValidationErrors.For("outcome", "Outcome must be success or fail.");

        var intent = await db.PaymentIntents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                i => i.Id == intentId && i.StudentId == studentId,
                cancellationToken)
            ?? throw new NotFoundException("Payment intent not found.");

        var succeeded = normalized == "success";
        var body = JsonSerializer.Serialize(
            new
            {
                providerReference = intent.ProviderReference ?? $"mock_{intent.Id}",
                succeeded,
                amount = intent.Amount,
                failureReason = succeeded ? null : "The payment was declined by the mock provider.",
            },
            Json);

        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(settings.MockSecret),
            Encoding.UTF8.GetBytes(body)));

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Mock-Signature"] = signature,
        };

        return await sender.Send(
            new HandlePaymentCallbackCommand(settings.Name, new HttpRequestData(headers, body)),
            cancellationToken);
    }
}
