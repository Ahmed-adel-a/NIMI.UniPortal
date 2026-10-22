using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Configurations;

/// <summary>library.loan_policies — one row per role once Student Affairs changes the defaults.</summary>
internal sealed class LoanPolicyConfiguration : IEntityTypeConfiguration<LoanPolicy>
{
    public void Configure(EntityTypeBuilder<LoanPolicy> builder)
    {
        builder.ToTable("loan_policies", table =>
        {
            table.HasCheckConstraint("ck_loan_policies_role", "role IN ('student','professor')");
            table.HasCheckConstraint("ck_loan_policies_values", "loan_days > 0 AND max_open_loans > 0 AND max_renewals >= 0 AND hold_days > 0");
        });
        builder.HasKey(p => p.Role);
        builder.Property(p => p.Role).HasMaxLength(20);
    }
}
