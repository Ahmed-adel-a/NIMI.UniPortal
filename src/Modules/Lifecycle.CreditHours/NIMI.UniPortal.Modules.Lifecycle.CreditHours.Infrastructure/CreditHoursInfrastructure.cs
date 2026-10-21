using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure;

public static class CreditHoursInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddCreditHoursInfrastructure(
        this IServiceCollection services, Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<CreditHoursDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<ICreditHoursDbContext>(provider => provider.GetRequiredService<CreditHoursDbContext>());
        services.AddScoped<IModuleDatabase, CreditHoursDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", CreditHoursDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
