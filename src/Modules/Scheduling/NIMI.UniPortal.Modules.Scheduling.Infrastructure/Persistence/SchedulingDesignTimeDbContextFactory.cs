using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class SchedulingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SchedulingDbContext>
{
    public SchedulingDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<SchedulingDbContext>)SchedulingInfrastructure
            .Configure(new DbContextOptionsBuilder<SchedulingDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
