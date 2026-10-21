using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Scheduling.Application.Behaviors;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application;

public static class SchedulingApplication
{
    public static IServiceCollection AddSchedulingApplication(this IServiceCollection services)
    {
        var assembly = typeof(SchedulingApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SchedulingUnitOfWorkBehavior<,>));
        services.AddScoped<ISchedulingApi, SchedulingApi>();
        services.AddScoped<SlotChangeNotifier>();
        return services;
    }
}
