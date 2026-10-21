using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Materials.Application.Behaviors;
using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application;

public static class MaterialsApplication
{
    public static IServiceCollection AddMaterialsApplication(this IServiceCollection services)
    {
        var assembly = typeof(MaterialsApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MaterialsUnitOfWorkBehavior<,>));
        services.AddScoped<CourseAccess>();
        return services;
    }
}
