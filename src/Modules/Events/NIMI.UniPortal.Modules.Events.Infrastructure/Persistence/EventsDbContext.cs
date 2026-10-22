using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Infrastructure.Persistence;

/// <summary>
/// The Events premium module's schema (P11, schema <c>events</c>). Register/cancel open one explicit transaction for
/// the seat counter; the override commits it after the UnitOfWork's single save, so seat and row change together.
/// </summary>
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options)
    : DbContext(options), IEventsDbContext
{
    public const string Schema = "events";

    public DbSet<CampusEvent> Events => Set<CampusEvent>();

    public DbSet<EventRegistration> Registrations => Set<EventRegistration>();

    internal DbSet<EventSeat> Seats => Set<EventSeat>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var saved = await base.SaveChangesAsync(cancellationToken);
        if (Database.CurrentTransaction is { } transaction)
            await transaction.CommitAsync(cancellationToken);
        return saved;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
    }
}
