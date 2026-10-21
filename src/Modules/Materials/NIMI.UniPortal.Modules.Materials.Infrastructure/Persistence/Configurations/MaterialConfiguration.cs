using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Materials.Domain;

namespace NIMI.UniPortal.Modules.Materials.Infrastructure.Persistence.Configurations;

/// <summary>materials.materials (phase §5).</summary>
internal sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials", table =>
        {
            table.HasCheckConstraint("ck_materials_file_type", "file_type IN ('pdf','ppt','doc','xls','zip','image','video','other')");
            table.HasCheckConstraint("ck_materials_week_number", "week_number IS NULL OR week_number BETWEEN 1 AND 30");
            table.HasCheckConstraint("ck_materials_size_bytes", "size_bytes > 0");
            table.HasCheckConstraint("ck_materials_download_count", "download_count >= 0");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Title).HasMaxLength(255);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.StorageKey).HasMaxLength(200);
        builder.Property(m => m.FileName).HasMaxLength(255);
        builder.Property(m => m.ContentType).HasMaxLength(100);
        builder.Property(m => m.FileType)
            .HasConversion(type => type.ToString().ToLowerInvariant(), text => Enum.Parse<MaterialFileType>(text, true))
            .HasMaxLength(10);
        builder.Property(m => m.DownloadCount).HasDefaultValue(0);
        builder.Property(m => m.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(m => new { m.SubjectId, m.TermId, m.IsDeleted });
    }
}
