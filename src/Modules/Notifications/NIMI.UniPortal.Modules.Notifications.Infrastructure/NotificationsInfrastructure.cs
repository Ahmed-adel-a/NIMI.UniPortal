using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.Modules.Notifications.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure;

public static class NotificationsInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services, Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<NotificationsDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<INotificationsDbContext>(provider => provider.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<IModuleDatabase, NotificationsDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
