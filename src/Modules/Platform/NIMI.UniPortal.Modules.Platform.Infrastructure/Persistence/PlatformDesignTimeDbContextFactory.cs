using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class PlatformDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<PlatformDbContext>)PlatformInfrastructure
            .Configure(new DbContextOptionsBuilder<PlatformDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
