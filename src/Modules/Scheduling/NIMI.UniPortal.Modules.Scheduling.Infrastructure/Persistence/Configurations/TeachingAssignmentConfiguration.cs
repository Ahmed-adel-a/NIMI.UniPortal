using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class TeachingAssignmentConfiguration : IEntityTypeConfiguration<TeachingAssignment>
{
    public void Configure(EntityTypeBuilder<TeachingAssignment> builder)
    {
        builder.ToTable("teaching_assignments", table =>
            table.HasCheckConstraint("ck_teaching_assignments_role", "role IN ('lecturer','lab','tutorial')"));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.StaffId);
        builder.Property(a => a.SubjectId);
        builder.Property(a => a.TermId);
        builder.Property(a => a.SubjectGroupId);
        builder.Property(a => a.Role)
            .HasConversion(new SnakeCaseEnumConverter<TeachingRole>())
            .HasMaxLength(10);
        builder.Property(a => a.AssignedBy);
        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(a => a.UpdatedAt);

        builder.HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Term)
            .WithMany()
            .HasForeignKey(a => a.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.SubjectGroup)
            .WithMany()
            .HasForeignKey(a => a.SubjectGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per staff + subject + term + group; a null group means the whole subject.
        builder.HasIndex(a => new { a.StaffId, a.SubjectId, a.TermId, a.SubjectGroupId })
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasIndex(a => a.StaffId);
        builder.HasIndex(a => a.SubjectId);
        builder.HasIndex(a => a.TermId);
        builder.HasIndex(a => a.SubjectGroupId);
    }
}
