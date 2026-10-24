using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.StudentReports.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the StudentReports schema without knowing the module.</summary>
internal sealed class StudentReportsDatabase(StudentReportsDbContext db) : IModuleDatabase
{
    public string Module => "StudentReports";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
