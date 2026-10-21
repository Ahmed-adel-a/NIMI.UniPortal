using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Audit.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class AuditDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<AuditDbContext>)AuditInfrastructure
            .Configure(new DbContextOptionsBuilder<AuditDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
