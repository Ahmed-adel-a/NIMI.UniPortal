using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Online.GetPaymentIntent;

internal sealed class GetPaymentIntentHandler(IPaymentsDbContext db, TimeProvider clock)
    : IRequestHandler<GetPaymentIntentQuery, PaymentIntentDto>
{
    public async Task<PaymentIntentDto> Handle(
        GetPaymentIntentQuery query, CancellationToken cancellationToken)
    {
        var intent = await db.PaymentIntents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                i => i.Id == query.IntentId && i.StudentId == query.StudentId,
                cancellationToken)
            ?? throw new NotFoundException("Payment intent not found.");

        var receipt = await db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentIntentId == intent.Id)
            .Select(p => p.ReceiptNumber)
            .SingleOrDefaultAsync(cancellationToken);

        return PaymentIntentDto.From(intent, intent.StatusAt(clock.GetUtcNow()), receipt);
    }
}
