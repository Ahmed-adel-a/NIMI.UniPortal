using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Application.Behaviors;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application;

public static class NotificationsApplication
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        var assembly = typeof(NotificationsApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(NotificationsUnitOfWorkBehavior<,>));
        services.AddIntegrationEventHandlers(assembly);   // NotificationRequested → stored notifications
        return services;
    }
}
