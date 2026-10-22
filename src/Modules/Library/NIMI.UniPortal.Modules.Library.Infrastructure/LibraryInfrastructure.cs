using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Infrastructure.Persistence;
using NIMI.UniPortal.Modules.Library.Infrastructure.Reminders;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Library.Infrastructure;

public static class LibraryInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddLibraryInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<LibraryDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<ILibraryDbContext>(provider => provider.GetRequiredService<LibraryDbContext>());
        services.AddScoped<IModuleDatabase, LibraryDatabase>();
        services.AddHostedService<LibraryReminderService>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", LibraryDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
