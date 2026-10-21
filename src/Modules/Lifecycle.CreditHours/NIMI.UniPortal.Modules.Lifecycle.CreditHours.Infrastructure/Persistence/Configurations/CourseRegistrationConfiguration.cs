using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Configurations;

internal sealed class CourseRegistrationConfiguration : IEntityTypeConfiguration<CourseRegistration>
{
    public void Configure(EntityTypeBuilder<CourseRegistration> builder)
    {
        builder.ToTable("course_registrations", table =>
            table.HasCheckConstraint(
                "ck_course_registrations_status",
                "status IN ('registered','pending_payment','dropped','completed','failed')"));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.StudentId);
        builder.Property(r => r.TermId);
        builder.Property(r => r.SubjectId);
        builder.Property(r => r.SubjectGroupId);
        builder.Property(r => r.CreditHours);
        builder.Property(r => r.Status)
            .HasConversion(new SnakeCaseEnumConverter<RegistrationStatus>())
            .HasMaxLength(20);
        builder.Property(r => r.RegisteredAt);
        builder.Property(r => r.DroppedAt);
        builder.Property(r => r.ConfirmedAt);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedAt);

        // Rule R4: one live registration per student, term and subject. Dropped rows stay as history.
        builder.HasIndex(r => new { r.StudentId, r.TermId, r.SubjectId })
            .IsUnique()
            .HasFilter("status <> 'dropped'");

        // Roster reads (R11) and seat accounting.
        builder.HasIndex(r => new { r.SubjectGroupId, r.Status });
    }
}
