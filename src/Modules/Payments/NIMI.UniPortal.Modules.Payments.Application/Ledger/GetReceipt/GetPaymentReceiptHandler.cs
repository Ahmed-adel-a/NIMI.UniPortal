using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetReceipt;

internal sealed class GetPaymentReceiptHandler(IPaymentsDbContext db, IUsersApi users)
    : IRequestHandler<GetPaymentReceiptQuery, PaymentReceiptDto>
{
    public async Task<PaymentReceiptDto> Handle(
        GetPaymentReceiptQuery query, CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == query.PaymentId, cancellationToken);

        if (payment is null || (!query.IsAdmin && payment.StudentId != query.CallerId))
            throw new NotFoundException("Payment not found.");

        var fee = await (
                from sf in db.StudentFees.AsNoTracking()
                join f in db.FeeItems.AsNoTracking() on sf.FeeItemId equals f.Id
                where sf.Id == payment.StudentFeeId
                select f)
            .SingleAsync(cancellationToken);

        var student = await users.GetUserAsync(payment.StudentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        return new PaymentReceiptDto(
            payment.Id,
            payment.ReceiptNumber,
            payment.StudentId,
            student.FullName,
            student.StudentCode,
            fee.Id,
            fee.Name,
            fee.NameAr,
            payment.Amount,
            PaymentMethodNames.ToWire(payment.Method),
            payment.Reference,
            payment.PaidAt);
    }
}
