using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Behaviors;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Lifecycle;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application;

public static class SemesterApplication
{
    public static IServiceCollection AddSemesterApplication(this IServiceCollection services)
    {
        var assembly = typeof(SemesterApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SemesterUnitOfWorkBehavior<,>));
        services.AddScoped<IAcademicLifecycle, AcademicLifecycleApi>();
        services.AddScoped<EffectivePolicies>();
        services.AddScoped<PromotionService>();
        services.AddIntegrationEventHandlers(assembly);   // GradesPublished → results + promotion; PaymentCompleted → held decisions
        return services;
    }
}
