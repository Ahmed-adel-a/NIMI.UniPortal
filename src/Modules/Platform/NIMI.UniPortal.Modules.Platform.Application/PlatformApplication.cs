using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Platform.Application.Behaviors;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application;

public static class PlatformApplication
{
    public static IServiceCollection AddPlatformApplication(this IServiceCollection services)
    {
        var assembly = typeof(PlatformApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PlatformUnitOfWorkBehavior<,>));
        return services;
    }
}
