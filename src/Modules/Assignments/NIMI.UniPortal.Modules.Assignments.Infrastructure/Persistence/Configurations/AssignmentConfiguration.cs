using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence.Configurations;

/// <summary>assignments.assignments (phase §4).</summary>
internal sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments", table =>
        {
            table.HasCheckConstraint("ck_assignments_max_grade", "max_grade > 0");
            table.HasCheckConstraint("ck_assignments_max_file_mb", "max_file_mb BETWEEN 1 AND 50");
        });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Title).HasMaxLength(255);
        builder.Property(a => a.Instructions).HasMaxLength(10000);
        builder.Property(a => a.MaxGrade).HasPrecision(5, 2);
        builder.Property(a => a.AllowedExtensions).HasMaxLength(200);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
        builder.Ignore(a => a.AllowedExtensionList);

        builder.HasIndex(a => new { a.SubjectId, a.TermId, a.IsDeleted });
    }
}
