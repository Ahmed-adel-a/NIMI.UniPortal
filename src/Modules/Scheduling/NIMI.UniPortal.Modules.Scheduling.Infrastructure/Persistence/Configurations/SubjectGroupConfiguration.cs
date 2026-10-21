using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class SubjectGroupConfiguration : IEntityTypeConfiguration<SubjectGroup>
{
    public void Configure(EntityTypeBuilder<SubjectGroup> builder)
    {
        builder.ToTable("subject_groups", table =>
        {
            table.HasCheckConstraint("ck_subject_groups_number", "group_number >= 1");
            table.HasCheckConstraint("ck_subject_groups_capacity", "capacity BETWEEN 1 AND 1000");
        });

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.SubjectId);
        builder.Property(g => g.TermId);
        builder.Property(g => g.GroupNumber);
        builder.Property(g => g.TargetDepartmentId);
        builder.Property(g => g.Capacity);
        builder.Property(g => g.IsActive).HasDefaultValue(true);
        builder.Property(g => g.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(g => g.UpdatedAt);

        builder.HasOne(g => g.Subject)
            .WithMany()
            .HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.Term)
            .WithMany()
            .HasForeignKey(g => g.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.SubjectId, g.TermId, g.GroupNumber }).IsUnique();
        builder.HasIndex(g => g.TermId);
        builder.HasIndex(g => g.TargetDepartmentId);
    }
}
