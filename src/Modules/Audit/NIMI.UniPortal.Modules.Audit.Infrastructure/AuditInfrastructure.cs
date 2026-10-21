using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Audit.Application.Abstractions;
using NIMI.UniPortal.Modules.Audit.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Audit.Infrastructure;

public static class AuditInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<AuditDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IAuditDbContext>(provider => provider.GetRequiredService<AuditDbContext>());
        services.AddScoped<IModuleDatabase, AuditDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", AuditDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
