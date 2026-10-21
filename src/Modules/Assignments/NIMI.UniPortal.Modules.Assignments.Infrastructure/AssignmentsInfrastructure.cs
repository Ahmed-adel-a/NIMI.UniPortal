using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence;
using NIMI.UniPortal.Modules.Assignments.Infrastructure.Receipts;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure;

public static class AssignmentsInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddAssignmentsInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<AssignmentsDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IAssignmentsDbContext>(provider => provider.GetRequiredService<AssignmentsDbContext>());
        services.AddScoped<IModuleDatabase, AssignmentsDatabase>();
        services.AddScoped<IReceiptAllocator, ReceiptAllocator>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", AssignmentsDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
