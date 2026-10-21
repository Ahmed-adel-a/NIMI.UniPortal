using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Configurations;

internal sealed class RegistrationWindowConfiguration : IEntityTypeConfiguration<RegistrationWindow>
{
    public void Configure(EntityTypeBuilder<RegistrationWindow> builder)
    {
        builder.ToTable("registration_windows", table =>
        {
            table.HasCheckConstraint("ck_registration_windows_dates", "closes_at > opens_at");
            table.HasCheckConstraint(
                "ck_registration_windows_credits",
                "min_credits >= 0 AND min_credits <= max_credits AND max_credits <= 40");
        });

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.TermId);
        builder.Property(w => w.OpensAt);
        builder.Property(w => w.ClosesAt);
        builder.Property(w => w.MinCredits);
        builder.Property(w => w.MaxCredits);
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(w => w.UpdatedAt);

        // Decision PREREQ: one window per term.
        builder.HasIndex(w => w.TermId).IsUnique();
    }
}
