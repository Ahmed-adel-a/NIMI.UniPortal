using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NIMI.UniPortal.EventBus;

/// <summary>
/// Resolves the handlers of the event's runtime type in registration order and invokes each in a new scope.
/// Exceptions are logged with the event type and id and never propagate (rule R2).
/// </summary>
internal sealed class EventPublisher(IServiceScopeFactory scopes, ILogger<EventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());

        foreach (var handlerType in HandlerTypes(handlerInterface))
        {
            await using var scope = scopes.CreateAsyncScope();
            try
            {
                // Handlers are registered against their interface (tests) or also as themselves (modules);
                // matching the concrete type keeps one instance of this handler in its own scope.
                var handler = scope.ServiceProvider.GetServices(handlerInterface)
                    .First(candidate => candidate!.GetType() == handlerType)!;
                await InvokeAsync(handler, handlerInterface, integrationEvent, cancellationToken);
            }
            catch (Exception exception)
            {
                var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
                logger.LogError(
                    cause,
                    "Integration event handler {Handler} failed for {EventType} ({EventId}); continuing with the next handler.",
                    handlerType.Name,
                    integrationEvent.GetType().Name,
                    integrationEvent.EventId);
            }
        }
    }

    /// <summary>A throwaway scope only to read the registered implementation types in registration order.</summary>
    private IReadOnlyList<Type> HandlerTypes(Type handlerInterface)
    {
        using var scope = scopes.CreateScope();
        return scope.ServiceProvider.GetServices(handlerInterface).Select(handler => handler!.GetType()).ToArray();
    }

    private static async Task InvokeAsync(
        object handler, Type handlerInterface, IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var method = handlerInterface.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;
        await (Task)method.Invoke(handler, [integrationEvent, cancellationToken])!;
    }
}
