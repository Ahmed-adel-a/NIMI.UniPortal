using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Infrastructure.Persistence;
using NIMI.UniPortal.Modules.Events.Infrastructure.Seats;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Events.Infrastructure;

public static class EventsInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddEventsInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<EventsDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IEventsDbContext>(provider => provider.GetRequiredService<EventsDbContext>());
        services.AddScoped<IModuleDatabase, EventsDatabase>();
        services.AddScoped<IEventSeats, EventSeats>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", EventsDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
