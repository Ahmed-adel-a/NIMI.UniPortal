using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Materials.Infrastructure;

public static class MaterialsInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddMaterialsInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<MaterialsDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IMaterialsDbContext>(provider => provider.GetRequiredService<MaterialsDbContext>());
        services.AddScoped<IModuleDatabase, MaterialsDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", MaterialsDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
