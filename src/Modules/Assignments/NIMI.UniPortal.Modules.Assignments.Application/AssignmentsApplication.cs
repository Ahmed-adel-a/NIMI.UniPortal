using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Assignments.Application.Behaviors;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.Modules.Assignments.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application;

public static class AssignmentsApplication
{
    public static IServiceCollection AddAssignmentsApplication(this IServiceCollection services)
    {
        var assembly = typeof(AssignmentsApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AssignmentsUnitOfWorkBehavior<,>));
        services.AddScoped<CourseAccess>();
        services.AddScoped<AssignmentAccess>();
        services.AddScoped<AssignmentPresenter>();
        services.AddScoped<IAssignmentsApi, AssignmentsApi>();
        return services;
    }
}
