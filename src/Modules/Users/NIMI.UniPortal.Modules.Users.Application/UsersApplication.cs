using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Users.Application.Behaviors;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application;

public static class UsersApplication
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        var assembly = typeof(UsersApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsersUnitOfWorkBehavior<,>));
        return services;
    }
}
