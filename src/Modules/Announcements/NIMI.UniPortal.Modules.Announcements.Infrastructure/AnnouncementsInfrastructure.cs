using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Announcements.Infrastructure;

public static class AnnouncementsInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddAnnouncementsInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<AnnouncementsDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IAnnouncementsDbContext>(provider => provider.GetRequiredService<AnnouncementsDbContext>());
        services.AddScoped<IModuleDatabase, AnnouncementsDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", AnnouncementsDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
