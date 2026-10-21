using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Contracts;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Payments.Domain.Ledger;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.RecordPayment;

internal sealed class RecordPaymentHandler(
    IPaymentsDbContext db,
    IUsersApi users,
    IPlatformSettingsApi platform,
    IReceiptNumberAllocator receipts,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<RecordPaymentCommand, PaymentRecordDto>
{
    public async Task<PaymentRecordDto> Handle(
        RecordPaymentCommand command, CancellationToken cancellationToken)
    {
        var student = await users.GetUserAsync(command.StudentId, cancellationToken);
        if (student is null || student.Role != PortalRoles.Student || !student.IsActive)
            throw ValidationErrors.For(nameof(command.StudentId), "Unknown or inactive student.");

        var placement = await users.GetStudentPlacementAsync(command.StudentId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.StudentId), "Unknown or inactive student.");

        var item = await db.FeeItems
            .SingleOrDefaultAsync(f => f.Id == command.FeeItemId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.FeeItemId), "Unknown fee item.");

        if (!PaymentMethodNames.TryParse(command.Method, out var method) || method == PaymentMethod.Online)
            throw ValidationErrors.For(nameof(command.Method), "Method must be cash or bank_transfer.");

        var now = clock.GetUtcNow();
        var fee = await db.StudentFees
            .SingleOrDefaultAsync(
                f => f.FeeItemId == command.FeeItemId && f.StudentId == command.StudentId,
                cancellationToken);

        // A military-education fee never "applies by targeting" (it exists only through its event-created row),
        // and a deactivated item still owes the rows created earlier. An existing row is therefore always payable.
        if (fee is null && !item.AppliesTo(placement.FacultyId, placement.DepartmentId))
            throw ValidationErrors.For(nameof(command.FeeItemId), "This fee does not apply to the student.");

        if (fee is null)
        {
            fee = StudentFee.Create(item, command.StudentId, now);
            db.StudentFees.Add(fee);
        }

        if (command.Amount > fee.Balance)
            throw ValidationErrors.For("amount", "Cannot exceed the outstanding balance.");

        fee.ApplyPayment(command.Amount, now);

        var settings = await platform.GetEffectiveSettingsAsync(placement.FacultyId, cancellationToken);
        var sequence = await receipts.NextAsync(EffectiveTime.Year(now, settings.TimeZone), cancellationToken);
        var receiptNumber = ReceiptNumber.Format(EffectiveTime.Year(now, settings.TimeZone), sequence);
        var paidAt = command.PaidAt?.ToUniversalTime() ?? now;

        var payment = Payment.Create(
            fee.Id,
            command.StudentId,
            command.Amount,
            method,
            command.Reference,
            paymentIntentId: null,
            receiptNumber,
            command.RecordedBy,
            paidAt,
            now);

        db.Payments.Add(payment);
        events.Add(new PaymentCompleted(
            Guid.CreateVersion7(),
            now,
            payment.Id,
            command.StudentId,
            item.Id,
            FeeCategoryNames.ToWire(item.Category),
            command.Amount,
            fee.IsFullyPaid));
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            now,
            command.RecordedBy,
            AuditActions.PaymentRecorded,
            AuditEntities.Payment,
            payment.Id,
            $"{PaymentMethodNames.ToWire(method)} payment of {command.Amount:0.00} for {item.Name} recorded for {student.FullName} (receipt {receiptNumber})."));

        return new PaymentRecordDto(
            payment.Id,
            payment.ReceiptNumber,
            command.StudentId,
            item.Id,
            payment.Amount,
            PaymentMethodNames.ToWire(payment.Method),
            payment.Reference,
            paidAt,
            EnumWire.ToWire(fee.Status),
            fee.Balance);
    }
}
