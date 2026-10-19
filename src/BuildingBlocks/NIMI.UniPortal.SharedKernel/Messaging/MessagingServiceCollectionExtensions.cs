using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NIMI.UniPortal.SharedKernel.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ISender"/>. Called once by the host.</summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.TryAddScoped<ISender, Sender>();
        return services;
    }

    /// <summary>
    /// Registers every concrete <see cref="IRequestHandler{TRequest,TResponse}"/> in the assembly (scoped).
    /// Each module's Application layer calls this for its own assembly.
    /// </summary>
    public static IServiceCollection AddRequestHandlers(this IServiceCollection services, Assembly assembly)
    {
        var registrations =
            from type in assembly.GetTypes()
            where type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
            select (contract, type);

        foreach (var (contract, implementation) in registrations)
        {
            if (services.Any(d => d.ServiceType == contract))
                throw new InvalidOperationException($"Two handlers for {contract.GenericTypeArguments[0].Name}: one request, one handler.");

            services.AddScoped(contract, implementation);
        }

        return services;
    }
}
