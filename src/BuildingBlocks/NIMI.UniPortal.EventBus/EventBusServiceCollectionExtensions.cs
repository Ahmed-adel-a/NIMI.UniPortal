using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NIMI.UniPortal.EventBus;

/// <summary>Registration for the in-process EventBus (02-architecture.md §7.1).</summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>Registers the scoped collector and publisher. Call once from the host, after <c>AddMessaging()</c>.</summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddScoped<IEventCollector, EventCollector>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        return services;
    }

    /// <summary>
    /// Registers every closed <see cref="IIntegrationEventHandler{TEvent}"/> implementation in the assembly
    /// as scoped — for its handler interface and for itself (the publisher resolves the implementation type
    /// in a fresh scope).
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (var handlerType in assembly.DefinedTypes.Where(type => !type.IsAbstract && !type.IsInterface))
        {
            var handlerInterfaces = handlerType.ImplementedInterfaces
                .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .ToArray();
            if (handlerInterfaces.Length == 0)
                continue;

            services.TryAddScoped(handlerType);
            foreach (var handlerInterface in handlerInterfaces)
                services.AddScoped(handlerInterface, provider => provider.GetRequiredService(handlerType));
        }

        return services;
    }
}
