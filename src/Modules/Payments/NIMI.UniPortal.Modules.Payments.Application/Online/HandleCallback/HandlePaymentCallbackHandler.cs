using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Contracts;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Payments.Domain.Ledger;
using NIMI.UniPortal.Modules.Payments.Domain.Online;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Online.HandleCallback;

internal sealed class HandlePaymentCallbackHandler(
    IPaymentsDbContext db,
    IPaymentGateway gateway,
    IUsersApi users,
    IPlatformSettingsApi platform,
    IReceiptNumberAllocator receipts,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<HandlePaymentCallbackCommand, PaymentIntentDto>
{
    public async Task<PaymentIntentDto> Handle(
        HandlePaymentCallbackCommand command, CancellationToken cancellationToken)
    {
        var callback = gateway.ParseAndVerifyCallback(command.Request);

        if (!string.Equals(command.Provider, gateway.Name, StringComparison.OrdinalIgnoreCase))
            throw new NotFoundException("Unknown payment provider.");

        var intent = await db.PaymentIntents
            .SingleOrDefaultAsync(
                i => i.Provider == gateway.Name && i.ProviderReference == callback.ProviderReference,
                cancellationToken)
            ?? throw new NotFoundException("Unknown payment intent.");

        var now = clock.GetUtcNow();
        var current = intent.StatusAt(now);
        if (current != PaymentIntentStatus.Pending)
            return PaymentIntentDto.From(intent, current, await ReceiptAsync(intent.Id, cancellationToken));

        if (!callback.Succeeded)
        {
            intent.Fail(callback.FailureReason ?? "The provider declined the payment.", now);
            NotifyFailure(intent.StudentId, intent.FailureReason!, now);
            return PaymentIntentDto.From(intent, PaymentIntentStatus.Failed, null);
        }

        if (callback.Amount != intent.Amount)
        {
            intent.Fail("The provider reported a different amount.", now);
            NotifyFailure(intent.StudentId, intent.FailureReason!, now);
            return PaymentIntentDto.From(intent, PaymentIntentStatus.Failed, null);
        }

        var placement = await users.GetStudentPlacementAsync(intent.StudentId, cancellationToken);
        var item = await db.FeeItems
            .SingleOrDefaultAsync(f => f.Id == intent.FeeItemId, cancellationToken);
        var fee = await db.StudentFees
            .SingleOrDefaultAsync(
                f => f.FeeItemId == intent.FeeItemId && f.StudentId == intent.StudentId,
                cancellationToken);

        // Military-education items only apply through their event-created row; an existing row stays payable
        // even if the item was deactivated after the checkout started.
        if (placement is null || item is null
            || (fee is null && !item.AppliesTo(placement.FacultyId, placement.DepartmentId)))
        {
            intent.Fail("The fee is no longer available.", now);
            NotifyFailure(intent.StudentId, intent.FailureReason!, now);
            return PaymentIntentDto.From(intent, PaymentIntentStatus.Failed, null);
        }

        if (fee is null)
        {
            fee = StudentFee.Create(item, intent.StudentId, now);
            db.StudentFees.Add(fee);
        }

        if (intent.Amount > fee.Balance)
        {
            intent.Fail("The fee has already been paid.", now);
            NotifyFailure(intent.StudentId, intent.FailureReason!, now);
            return PaymentIntentDto.From(intent, PaymentIntentStatus.Failed, null);
        }

        fee.ApplyPayment(intent.Amount, now);

        var settings = await platform.GetEffectiveSettingsAsync(placement.FacultyId, cancellationToken);
        var year = EffectiveTime.Year(now, settings.TimeZone);
        var sequence = await receipts.NextAsync(year, cancellationToken);
        var receiptNumber = ReceiptNumber.Format(year, sequence);

        var payment = Payment.Create(
            fee.Id,
            intent.StudentId,
            intent.Amount,
            PaymentMethod.Online,
            reference: null,
            intent.Id,
            receiptNumber,
            intent.StudentId,
            now,
            now);

        db.Payments.Add(payment);
        intent.Succeed(now);

        events.Add(new PaymentCompleted(
            Guid.CreateVersion7(),
            now,
            payment.Id,
            intent.StudentId,
            item.Id,
            FeeCategoryNames.ToWire(item.Category),
            intent.Amount,
            fee.IsFullyPaid));

        return PaymentIntentDto.From(intent, PaymentIntentStatus.Succeeded, receiptNumber);
    }

    private void NotifyFailure(Guid studentId, string reason, DateTimeOffset now) =>
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            [studentId],
            "payment",
            "Online payment failed",
            "فشل الدفع الإلكتروني",
            $"Your online payment failed: {reason}",
            $"فشل دفعك الإلكتروني: {reason}",
            "/payments"));

    private Task<string?> ReceiptAsync(Guid intentId, CancellationToken cancellationToken) =>
        db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentIntentId == intentId)
            .Select(p => p.ReceiptNumber)
            .SingleOrDefaultAsync(cancellationToken);
}
