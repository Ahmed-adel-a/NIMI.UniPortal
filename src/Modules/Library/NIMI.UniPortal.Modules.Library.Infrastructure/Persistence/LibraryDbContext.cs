using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.Modules.Library.Domain.Digital;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence;

/// <summary>The Library premium module's own schema (P11, schema <c>library</c>) — ids of other modules only.</summary>
public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : DbContext(options), ILibraryDbContext
{
    public const string Schema = "library";

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookCopy> Copies => Set<BookCopy>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<LoanPolicy> Policies => Set<LoanPolicy>();

    public DbSet<LoanReminder> Reminders => Set<LoanReminder>();

    public DbSet<DigitalBook> DigitalBooks => Set<DigitalBook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
