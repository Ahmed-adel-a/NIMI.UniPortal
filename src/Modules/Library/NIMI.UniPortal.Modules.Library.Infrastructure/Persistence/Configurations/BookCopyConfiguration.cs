using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Configurations;

/// <summary>library.book_copies — unique barcodes; xmin so two desks can't lend the same copy at once.</summary>
internal sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("book_copies", table =>
            table.HasCheckConstraint("ck_book_copies_status", "status IN ('available','on_loan','on_hold','lost','withdrawn')"));
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.HasOne<Book>().WithMany().HasForeignKey(c => c.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(c => c.Barcode).HasMaxLength(BookCopy.MaxBarcodeLength);
        builder.Property(c => c.Shelf).HasMaxLength(BookCopy.MaxShelfLength);
        builder.Property(c => c.Status).HasConversion(new SnakeCaseEnumConverter<CopyStatus>()).HasMaxLength(20);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(c => c.Barcode).IsUnique();
        builder.HasIndex(c => new { c.BookId, c.Status });
    }
}
