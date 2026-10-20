using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Users.Domain.Organization;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Code).HasMaxLength(10);
        builder.Property(d => d.Name).HasMaxLength(255);
        builder.Property(d => d.NameAr).HasMaxLength(255);
        builder.Property(d => d.Description).HasMaxLength(2000);
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");

        // Same module → a real foreign key. Restrict: faculties are deactivated, never deleted.
        builder.HasOne<Faculty>().WithMany().HasForeignKey(d => d.FacultyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.Code).IsUnique();   // department codes are unique across the university
    }
}
