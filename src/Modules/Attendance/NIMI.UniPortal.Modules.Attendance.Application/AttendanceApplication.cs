using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Attendance.Application.Behaviors;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application;

public static class AttendanceApplication
{
    public static IServiceCollection AddAttendanceApplication(this IServiceCollection services)
    {
        var assembly = typeof(AttendanceApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AttendanceUnitOfWorkBehavior<,>));
        return services;
    }
}
