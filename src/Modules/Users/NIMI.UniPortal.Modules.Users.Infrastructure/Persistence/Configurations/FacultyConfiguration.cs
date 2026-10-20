using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Users.Domain.Organization;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Configurations;

internal sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("faculties");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Code).HasMaxLength(10);
        builder.Property(f => f.Name).HasMaxLength(255);
        builder.Property(f => f.NameAr).HasMaxLength(255);
        builder.Property(f => f.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(f => f.Code).IsUnique();   // backstop for the handler's duplicate check (→ 409)
    }
}
