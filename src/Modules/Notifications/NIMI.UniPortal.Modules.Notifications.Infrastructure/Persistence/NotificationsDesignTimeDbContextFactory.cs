using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class NotificationsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<NotificationsDbContext>)NotificationsInfrastructure
            .Configure(new DbContextOptionsBuilder<NotificationsDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
