using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Notifications.Application;
using NIMI.UniPortal.Modules.Notifications.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Notifications.Api;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddNotificationsApplication();
        services.AddNotificationsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }
}
