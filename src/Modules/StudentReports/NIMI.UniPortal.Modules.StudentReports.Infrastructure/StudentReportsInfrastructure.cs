using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.StudentReports.Infrastructure;

public static class StudentReportsInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddStudentReportsInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<StudentReportsDbContext>((provider, options) => Configure(options, connectionString(provider)));
        services.AddScoped<IStudentReportsDbContext>(provider => provider.GetRequiredService<StudentReportsDbContext>());
        services.AddScoped<IModuleDatabase, StudentReportsDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", StudentReportsDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
