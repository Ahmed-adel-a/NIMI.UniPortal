using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Grades.Infrastructure;

public static class GradesInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddGradesInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<GradesDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IGradesDbContext>(provider => provider.GetRequiredService<GradesDbContext>());
        services.AddScoped<IModuleDatabase, GradesDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", GradesDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
