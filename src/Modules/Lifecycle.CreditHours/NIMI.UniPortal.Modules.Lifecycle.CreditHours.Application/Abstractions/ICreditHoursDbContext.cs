using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;

/// <summary>
/// The CreditHours module's data seam (P4). Commands mutate tracked entities and let the module's UnitOfWork save
/// exactly once; raw SQL is used only for the atomic seat counter, inside the transaction the register/drop command
/// opens on <see cref="Database"/>.
/// </summary>
public interface ICreditHoursDbContext
{
    DbSet<RegistrationWindow> RegistrationWindows { get; }

    DbSet<SubjectPrerequisite> SubjectPrerequisites { get; }

    DbSet<GroupSeat> GroupSeats { get; }

    DbSet<CourseRegistration> CourseRegistrations { get; }

    DbSet<CreditLedgerEntry> CreditLedger { get; }

    /// <summary>
    /// The relational facade: the seat counter is raw SQL, and register/drop open one explicit transaction that the
    /// DbContext commits together with the command's single <see cref="SaveChangesAsync"/>.
    /// </summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
