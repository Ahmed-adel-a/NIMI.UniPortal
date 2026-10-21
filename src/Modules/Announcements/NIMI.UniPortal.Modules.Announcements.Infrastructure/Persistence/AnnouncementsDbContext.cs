using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Domain;

namespace NIMI.UniPortal.Modules.Announcements.Infrastructure.Persistence;

/// <summary>The Announcements module's own schema (P7, schema <c>announcements</c>) — ids of other modules only.</summary>
public sealed class AnnouncementsDbContext(DbContextOptions<AnnouncementsDbContext> options)
    : DbContext(options), IAnnouncementsDbContext
{
    public const string Schema = "announcements";

    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnnouncementsDbContext).Assembly);
    }
}
