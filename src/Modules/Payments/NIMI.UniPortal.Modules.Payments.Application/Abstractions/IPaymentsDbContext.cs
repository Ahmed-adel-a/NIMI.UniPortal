using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Payments.Domain.Ledger;
using NIMI.UniPortal.Modules.Payments.Domain.Online;

namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>The Payments module's single data seam (no repositories — solution-structure.md §6).</summary>
public interface IPaymentsDbContext
{
    DbSet<FeeItem> FeeItems { get; }

    DbSet<StudentFee> StudentFees { get; }

    DbSet<Payment> Payments { get; }

    DbSet<PaymentIntent> PaymentIntents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
