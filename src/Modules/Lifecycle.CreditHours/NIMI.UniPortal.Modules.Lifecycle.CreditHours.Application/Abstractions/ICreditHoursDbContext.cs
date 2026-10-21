using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Graduation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Results;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;

/// <summary>
/// The CreditHours module's data seam (P4). Commands mutate tracked entities and let the module's UnitOfWork save
/// exactly once; the atomic seat counter is the separate <see cref="ISeatCounter"/> port.
/// </summary>
public interface ICreditHoursDbContext
{
    DbSet<RegistrationWindow> RegistrationWindows { get; }

    DbSet<SubjectPrerequisite> SubjectPrerequisites { get; }

    DbSet<GroupSeat> GroupSeats { get; }

    DbSet<CourseRegistration> CourseRegistrations { get; }

    DbSet<CreditLedgerEntry> CreditLedger { get; }

    DbSet<GpaSnapshot> GpaSnapshots { get; }

    DbSet<GraduationRequirement> GraduationRequirements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
