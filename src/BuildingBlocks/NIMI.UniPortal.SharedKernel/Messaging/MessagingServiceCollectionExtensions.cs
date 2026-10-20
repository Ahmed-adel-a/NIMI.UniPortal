using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NIMI.UniPortal.SharedKernel.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ISender"/> and the validation behavior. Called once by the host, <b>before</b> any module,
    /// so validation is the outermost behavior (it runs before a module's UnitOfWork).
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.TryAddScoped<ISender, Sender>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }

    /// <summary>
    /// Registers every concrete <see cref="IRequestHandler{TRequest,TResponse}"/> in the assembly (scoped).
    /// Each module's Application layer calls this for its own assembly.
    /// </summary>
    public static IServiceCollection AddRequestHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (var (contract, implementation) in ClosedImplementations(assembly, typeof(IRequestHandler<,>)))
        {
            if (services.Any(d => d.ServiceType == contract))
                throw new InvalidOperationException($"Two handlers for {contract.GenericTypeArguments[0].Name}: one request, one handler.");

            services.AddScoped(contract, implementation);
        }

        return services;
    }

    /// <summary>Registers every concrete FluentValidation <see cref="IValidator{T}"/> in the assembly (scoped).</summary>
    public static IServiceCollection AddValidators(this IServiceCollection services, Assembly assembly)
    {
        foreach (var (contract, implementation) in ClosedImplementations(assembly, typeof(IValidator<>)))
            services.AddScoped(contract, implementation);

        return services;
    }

    private static IEnumerable<(Type Contract, Type Implementation)> ClosedImplementations(Assembly assembly, Type openContract) =>
        from type in assembly.GetTypes()
        where type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
        from contract in type.GetInterfaces()
        where contract.IsGenericType && contract.GetGenericTypeDefinition() == openContract
        select (contract, type);
}
