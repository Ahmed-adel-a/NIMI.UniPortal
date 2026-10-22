using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Application.Abstractions;

/// <summary>The Library module's data seam (P11). Commands change tracked entities; the module's UnitOfWork saves once.</summary>
public interface ILibraryDbContext
{
    DbSet<Book> Books { get; }

    DbSet<BookCopy> Copies { get; }

    DbSet<Loan> Loans { get; }

    DbSet<Reservation> Reservations { get; }

    DbSet<LoanPolicy> Policies { get; }

    DbSet<LoanReminder> Reminders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
