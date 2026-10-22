using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.Modules.Library.Domain.Digital;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Configurations;

/// <summary>library.digital_books: verified e-books (DIGILIB); removing one sets removed_at.</summary>
internal sealed class DigitalBookConfiguration : IEntityTypeConfiguration<DigitalBook>
{
    public void Configure(EntityTypeBuilder<DigitalBook> builder)
    {
        builder.ToTable("digital_books", table =>
            table.HasCheckConstraint("ck_digital_books_format", "format IN ('pdf','epub')"));

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Title).HasMaxLength(Book.MaxTitleLength);
        builder.Property(b => b.Authors).HasMaxLength(Book.MaxAuthorsLength);
        builder.Property(b => b.Description).HasMaxLength(DigitalBook.MaxDescriptionLength);
        builder.Property(b => b.Format).HasConversion(new SnakeCaseEnumConverter<DigitalBookFormat>()).HasMaxLength(10);
        builder.Property(b => b.FileKey).HasMaxLength(300);
        builder.Property(b => b.FileName).HasMaxLength(255);

        builder.HasIndex(b => new { b.FacultyId, b.DepartmentId });
        builder.HasIndex(b => b.SubjectId);
    }
}
