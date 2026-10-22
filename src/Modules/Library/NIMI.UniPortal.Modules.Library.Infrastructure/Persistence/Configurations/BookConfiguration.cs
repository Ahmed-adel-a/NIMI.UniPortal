using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Configurations;

/// <summary>library.books — an ISBN appears once.</summary>
internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Isbn).HasMaxLength(13);
        builder.Property(b => b.Title).HasMaxLength(Book.MaxTitleLength);
        builder.Property(b => b.Authors).HasMaxLength(Book.MaxAuthorsLength);
        builder.Property(b => b.Publisher).HasMaxLength(Book.MaxPublisherLength);
        builder.HasIndex(b => b.Isbn).IsUnique().HasFilter("isbn IS NOT NULL");
        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.FacultyId);
    }
}
