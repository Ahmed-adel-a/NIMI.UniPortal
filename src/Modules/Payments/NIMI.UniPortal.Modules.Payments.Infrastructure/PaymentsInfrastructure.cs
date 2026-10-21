using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Infrastructure.Payments;
using NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure;

public static class PaymentsInfrastructure
{
    /// <param name="connectionString">Resolved lazily, so configuration overrides (user secrets, env, tests) always win.</param>
    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<PaymentsDbContext>((provider, options) =>
            Configure(options, connectionString(provider)));
        services.AddScoped<IPaymentsDbContext>(provider => provider.GetRequiredService<PaymentsDbContext>());
        services.AddScoped<IModuleDatabase, PaymentsDatabase>();
        services.AddScoped<IReceiptNumberAllocator, ReceiptNumberAllocator>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PaymentsDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
