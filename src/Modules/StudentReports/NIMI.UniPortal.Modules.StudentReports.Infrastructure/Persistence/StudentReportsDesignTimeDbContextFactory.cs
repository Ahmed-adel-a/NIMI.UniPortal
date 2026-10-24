using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.StudentReports.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class StudentReportsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<StudentReportsDbContext>
{
    public StudentReportsDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<StudentReportsDbContext>)StudentReportsInfrastructure
            .Configure(new DbContextOptionsBuilder<StudentReportsDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
