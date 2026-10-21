using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class CreditHoursDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CreditHoursDbContext>
{
    public CreditHoursDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<CreditHoursDbContext>)CreditHoursInfrastructure
            .Configure(new DbContextOptionsBuilder<CreditHoursDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
