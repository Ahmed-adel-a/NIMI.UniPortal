using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Payments.Domain.Ledger;
using NIMI.UniPortal.Modules.Payments.Domain.Online;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options), IPaymentsDbContext
{
    public const string Schema = "payments";

    public DbSet<FeeItem> FeeItems => Set<FeeItem>();

    public DbSet<StudentFee> StudentFees => Set<StudentFee>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
