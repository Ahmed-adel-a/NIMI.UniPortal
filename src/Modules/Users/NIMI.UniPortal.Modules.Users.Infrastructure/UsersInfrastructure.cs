using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Users.Infrastructure;

public static class UsersInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services, Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<UsersDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IUsersDbContext>(provider => provider.GetRequiredService<UsersDbContext>());
        services.AddScoped<IModuleDatabase, UsersDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", UsersDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
