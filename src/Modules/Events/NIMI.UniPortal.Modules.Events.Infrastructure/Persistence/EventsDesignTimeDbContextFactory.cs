using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Events.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class EventsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<EventsDbContext>)EventsInfrastructure
            .Configure(new DbContextOptionsBuilder<EventsDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
