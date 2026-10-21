using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class AttendanceDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AttendanceDbContext>
{
    public AttendanceDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<AttendanceDbContext>)AttendanceInfrastructure
            .Configure(new DbContextOptionsBuilder<AttendanceDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
