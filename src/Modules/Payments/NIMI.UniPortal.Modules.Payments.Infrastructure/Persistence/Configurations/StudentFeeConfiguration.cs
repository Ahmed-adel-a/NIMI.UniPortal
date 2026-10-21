using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence.Configurations;

internal sealed class StudentFeeConfiguration : IEntityTypeConfiguration<StudentFee>
{
    public void Configure(EntityTypeBuilder<StudentFee> builder)
    {
        builder.ToTable("student_fees", table =>
        {
            table.HasCheckConstraint("ck_student_fees_amount_due", "amount_due > 0");
            table.HasCheckConstraint("ck_student_fees_amount_paid", "amount_paid >= 0 AND amount_paid <= amount_due");
            table.HasCheckConstraint(
                "ck_student_fees_status",
                "status IN ('pending','partially_paid','paid')");
        });

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.FeeItemId);
        builder.Property(f => f.StudentId);
        builder.Property(f => f.AmountDue).HasPrecision(12, 2);
        builder.Property(f => f.AmountPaid).HasPrecision(12, 2).HasDefaultValue(0m);
        builder.Property(f => f.Status)
            .HasConversion(new SnakeCaseEnumConverter<StudentFeeStatus>())
            .HasMaxLength(20);
        builder.Property(f => f.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(f => f.UpdatedAt);

        builder.Ignore(f => f.Balance);
        builder.Ignore(f => f.IsFullyPaid);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne<FeeItem>()
            .WithMany()
            .HasForeignKey(f => f.FeeItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.FeeItemId, f.StudentId }).IsUnique();
        builder.HasIndex(f => f.StudentId);
    }
}
