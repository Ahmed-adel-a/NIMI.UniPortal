using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Grades.Application.Behaviors;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Application.Drafting;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application;

public static class GradesApplication
{
    public static IServiceCollection AddGradesApplication(this IServiceCollection services)
    {
        var assembly = typeof(GradesApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(GradesUnitOfWorkBehavior<,>));
        services.AddScoped<GradingAccess>();
        services.AddScoped<GradeScales>();
        services.AddScoped<GroupGradesPresenter>();
        services.AddScoped<GradeDrafter>();   // typed drafts and CSV imports share one set of rules
        services.AddScoped<IGradesApi, GradesApi>();
        return services;
    }
}
