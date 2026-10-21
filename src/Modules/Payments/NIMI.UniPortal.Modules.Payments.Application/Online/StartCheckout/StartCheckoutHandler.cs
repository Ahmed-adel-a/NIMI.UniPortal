using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Payments.Domain.Online;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Payments.Application.Online.StartCheckout;

internal sealed class StartCheckoutHandler(
    IPaymentsDbContext db,
    IPaymentGateway gateway,
    IUsersApi users,
    TimeProvider clock) : IRequestHandler<StartCheckoutCommand, CheckoutDto>
{
    public async Task<CheckoutDto> Handle(StartCheckoutCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsStudent)
            throw new ForbiddenException("Only students can pay online.");

        var placement = await users.GetStudentPlacementAsync(command.StudentId, cancellationToken)
            ?? throw new ForbiddenException("Only students can pay online.");

        if (!placement.IsActive)
            throw new ForbiddenException("Only active students can pay online.");

        var item = await db.FeeItems
            .SingleOrDefaultAsync(f => f.Id == command.FeeItemId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.FeeItemId), "Unknown fee item.");

        var fee = await db.StudentFees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                f => f.FeeItemId == command.FeeItemId && f.StudentId == command.StudentId,
                cancellationToken);

        // Military-education fees are payable only through their event-created row; an existing row is also
        // payable after the item has been deactivated (the debt does not disappear).
        if (fee is null && !item.AppliesTo(placement.FacultyId, placement.DepartmentId))
            throw ValidationErrors.For(nameof(command.FeeItemId), "This fee does not apply to the student.");

        var balance = fee?.Balance ?? item.Amount;
        if (command.Amount > balance)
            throw ValidationErrors.For("amount", "Cannot exceed the outstanding balance.");

        var now = clock.GetUtcNow();
        var pending = await db.PaymentIntents
            .AsNoTracking()
            .AnyAsync(
                i => i.StudentId == command.StudentId
                     && i.FeeItemId == command.FeeItemId
                     && i.Status == PaymentIntentStatus.Pending
                     && i.ExpiresAt > now,
                cancellationToken);

        if (pending)
            throw new ConflictException("A checkout for this fee is already pending.");

        var intent = PaymentIntent.Create(
            command.StudentId, command.FeeItemId, command.Amount, gateway.Name, now);

        var session = await gateway.CreateCheckoutAsync(
            new CheckoutRequest(
                intent.Id,
                command.Amount,
                "EGP",
                item.Name,
                new Uri($"/api/payments/intents/{intent.Id}", UriKind.Relative)),
            cancellationToken);

        intent.AttachProviderReference(session.ProviderReference);
        db.PaymentIntents.Add(intent);

        return new CheckoutDto(intent.Id, session.RedirectUrl.ToString());
    }
}
