using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Behaviors;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Lifecycle;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application;

public static class CreditHoursApplication
{
    public static IServiceCollection AddCreditHoursApplication(this IServiceCollection services)
    {
        var assembly = typeof(CreditHoursApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CreditHoursUnitOfWorkBehavior<,>));
        services.AddScoped<IAcademicLifecycle, AcademicLifecycleApi>();
        return services;
    }
}
