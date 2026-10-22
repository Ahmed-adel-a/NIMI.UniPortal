using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options), INotificationsDbContext
{
    public const string Schema = "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<OutboundEmail> OutboundEmails => Set<OutboundEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
