using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Grades.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class GradesDesignTimeDbContextFactory : IDesignTimeDbContextFactory<GradesDbContext>
{
    public GradesDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<GradesDbContext>)GradesInfrastructure
            .Configure(new DbContextOptionsBuilder<GradesDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
