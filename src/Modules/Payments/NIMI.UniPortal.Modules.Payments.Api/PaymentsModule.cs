using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NIMI.UniPortal.Modules.Payments.Api.Endpoints;
using NIMI.UniPortal.Modules.Payments.Application;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Payments.Api;

public static class PaymentsModule
{
    /// <summary>
    /// Wires Payments. PAYGW: the gateway defaults to <c>mock</c> outside Production; Production refuses to start
    /// with the mock and with a provider that has no adapter yet (PAYPROVIDER is still open).
    /// </summary>
    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configured = configuration["Payments:Gateway"];
        var gateway = string.IsNullOrWhiteSpace(configured)
            ? (environment.IsProduction()
                ? throw new InvalidOperationException(
                    "Payments:Gateway must be configured outside Development/Testing; the mock gateway is not allowed in Production.")
                : "mock")
            : configured.Trim().ToLowerInvariant();

        if (environment.IsProduction() && gateway == "mock")
            throw new InvalidOperationException(
                "Payments:Gateway=mock is not allowed in Production; configure a real provider.");

        if (gateway != "mock")
            throw new InvalidOperationException(
                $"Payments:Gateway='{gateway}' has no adapter in this build (decision PAYPROVIDER is still open).");

        var mockSecret = configuration["Payments:Mock:Secret"];
        var settings = new PaymentsGatewaySettings(
            gateway,
            string.IsNullOrWhiteSpace(mockSecret) ? "nimi-mock-secret" : mockSecret);

        services.AddSingleton(settings);
        services.AddPaymentsApplication();
        services.AddPaymentsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        FeeItemEndpoints.Map(endpoints);
        StudentFeeEndpoints.Map(endpoints);
        OfficePaymentEndpoints.Map(endpoints);
        HistoryEndpoints.Map(endpoints);

        var settings = endpoints.ServiceProvider.GetRequiredService<PaymentsGatewaySettings>();
        OnlinePaymentEndpoints.Map(endpoints, settings.IsMock);
        return endpoints;
    }
}
