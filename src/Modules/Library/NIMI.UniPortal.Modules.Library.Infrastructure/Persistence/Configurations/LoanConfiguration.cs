using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Configurations;

/// <summary>library.loans — at most one open loan per copy (partial unique index).</summary>
internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans", table => table.HasCheckConstraint("ck_loans_renewals", "renewals >= 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.HasOne<BookCopy>().WithMany().HasForeignKey(l => l.CopyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Book>().WithMany().HasForeignKey(l => l.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(l => l.IsOpen);
        builder.HasIndex(l => l.CopyId).IsUnique().HasFilter("returned_at IS NULL").HasDatabaseName("ux_loans_open_copy");
        builder.HasIndex(l => new { l.UserId, l.ReturnedAt });
        builder.HasIndex(l => l.DueAt);
    }
}
