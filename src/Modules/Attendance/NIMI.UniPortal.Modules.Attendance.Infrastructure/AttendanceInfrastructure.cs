using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure;

public static class AttendanceInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddAttendanceInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<AttendanceDbContext>((provider, options) =>
            Configure(options, connectionString(provider)));
        services.AddScoped<IAttendanceDbContext>(provider => provider.GetRequiredService<AttendanceDbContext>());
        services.AddScoped<IModuleDatabase, AttendanceDatabase>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", AttendanceDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
