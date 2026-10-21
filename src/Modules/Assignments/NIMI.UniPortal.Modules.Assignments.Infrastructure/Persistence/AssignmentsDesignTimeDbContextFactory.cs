using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class AssignmentsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AssignmentsDbContext>
{
    public AssignmentsDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<AssignmentsDbContext>)AssignmentsInfrastructure
            .Configure(new DbContextOptionsBuilder<AssignmentsDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
