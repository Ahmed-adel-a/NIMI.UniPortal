using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure;

public static class SchedulingInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddSchedulingInfrastructure(
        this IServiceCollection services, Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<SchedulingDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<ISchedulingDbContext>(provider => provider.GetRequiredService<SchedulingDbContext>());
        services.AddScoped<IModuleDatabase, SchedulingDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", SchedulingDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
