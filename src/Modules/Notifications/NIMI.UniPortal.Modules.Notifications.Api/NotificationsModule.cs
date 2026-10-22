using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NIMI.UniPortal.Modules.Notifications.Api.Endpoints;
using NIMI.UniPortal.Modules.Notifications.Application;
using NIMI.UniPortal.Modules.Notifications.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Notifications.Api;

public static class NotificationsModule
{
    private const string OutboxProvider = "outbox";

    /// <summary>
    /// <c>Email:Provider</c> picks the email provider (decision ACCOUNTX). Only <c>outbox</c> exists in this build
    /// (EMAILPROVIDER is open): the default outside Production, refused in Production.
    /// </summary>
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Email:Provider"];
        var provider = string.IsNullOrWhiteSpace(configured)
            ? (environment.IsProduction()
                ? throw new InvalidOperationException("Email:Provider must be configured in Production; the outbox provider sends nothing.")
                : OutboxProvider)
            : configured.Trim().ToLowerInvariant();
        if (environment.IsProduction() && provider == OutboxProvider)
            throw new InvalidOperationException("Email:Provider=outbox is not allowed in Production; configure a real provider.");
        if (provider != OutboxProvider)
            throw new InvalidOperationException($"Email:Provider='{provider}' has no adapter in this build (decision EMAILPROVIDER is still open).");

        services.AddNotificationsApplication();
        services.AddNotificationsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        NotificationEndpoints.Map(endpoints);

        // The outbox lets a frontend developer follow emails (e.g. reset links) without a mail server.
        var environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        if (environment.IsDevelopment() ||
            string.Equals(configuration["Email:ExposeOutbox"], "true", StringComparison.OrdinalIgnoreCase))
            OutboxEndpoints.Map(endpoints);

        return endpoints;
    }
}
