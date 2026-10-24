using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.StudentReports.Application.Behaviors;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.StudentReports.Application;

public static class StudentReportsApplication
{
    public static IServiceCollection AddStudentReportsApplication(this IServiceCollection services)
    {
        var assembly = typeof(StudentReportsApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(StudentReportsUnitOfWorkBehavior<,>));
        services.AddScoped<HeadAccess>();
        services.AddScoped<ReportPresenter>();
        return services;
    }
}
