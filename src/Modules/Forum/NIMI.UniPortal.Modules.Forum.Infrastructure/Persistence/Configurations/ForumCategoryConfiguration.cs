using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence.Configurations;

/// <summary>forum.categories.</summary>
internal sealed class ForumCategoryConfiguration : IEntityTypeConfiguration<ForumCategory>
{
    public void Configure(EntityTypeBuilder<ForumCategory> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(ForumCategory.MaxNameLength);
        builder.Property(c => c.NameAr).HasMaxLength(ForumCategory.MaxNameLength);
        builder.HasIndex(c => new { c.IsActive, c.FacultyId });
    }
}
