using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Attendance schema without knowing the module.</summary>
internal sealed class AttendanceDatabase(AttendanceDbContext db) : IModuleDatabase
{
    public string Module => "Attendance";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
