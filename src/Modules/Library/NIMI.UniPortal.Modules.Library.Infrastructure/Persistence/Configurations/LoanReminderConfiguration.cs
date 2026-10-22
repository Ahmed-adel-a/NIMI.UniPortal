using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Configurations;

/// <summary>library.loan_reminders — one reminder per loan, kind and day (LIBREMIND).</summary>
internal sealed class LoanReminderConfiguration : IEntityTypeConfiguration<LoanReminder>
{
    public void Configure(EntityTypeBuilder<LoanReminder> builder)
    {
        builder.ToTable("loan_reminders", table => table.HasCheckConstraint("ck_loan_reminders_kind", "kind IN ('due_soon','overdue')"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasOne<Loan>().WithMany().HasForeignKey(r => r.LoanId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(r => r.Kind).HasConversion(new SnakeCaseEnumConverter<ReminderKind>()).HasMaxLength(20);
        builder.HasIndex(r => new { r.LoanId, r.Kind, r.SentOn }).IsUnique();
    }
}
