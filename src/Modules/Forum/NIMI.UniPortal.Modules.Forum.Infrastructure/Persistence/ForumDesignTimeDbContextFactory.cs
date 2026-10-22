using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class ForumDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ForumDbContext>
{
    public ForumDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<ForumDbContext>)ForumInfrastructure
            .Configure(new DbContextOptionsBuilder<ForumDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
