using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Events.Application.Behaviors;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application;

public static class EventsApplication
{
    public static IServiceCollection AddEventsApplication(this IServiceCollection services)
    {
        var assembly = typeof(EventsApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventsUnitOfWorkBehavior<,>));
        services.AddScoped<EventAudience>();
        services.AddScoped<EventPresenter>();
        services.AddScoped<RegistrantNotices>();
        return services;
    }
}
