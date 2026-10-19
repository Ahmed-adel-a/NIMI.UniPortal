using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application;

public static class PlatformApplication
{
    public static IServiceCollection AddPlatformApplication(this IServiceCollection services) =>
        services.AddRequestHandlers(typeof(PlatformApplication).Assembly);
}
