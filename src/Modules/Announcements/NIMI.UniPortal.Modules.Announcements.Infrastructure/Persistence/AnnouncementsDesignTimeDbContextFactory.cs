using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Announcements.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class AnnouncementsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AnnouncementsDbContext>
{
    public AnnouncementsDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<AnnouncementsDbContext>)AnnouncementsInfrastructure
            .Configure(new DbContextOptionsBuilder<AnnouncementsDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
