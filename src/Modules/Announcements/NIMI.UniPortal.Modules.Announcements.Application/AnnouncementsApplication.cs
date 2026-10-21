using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Announcements.Application.Behaviors;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Announcements.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Announcements.Application;

public static class AnnouncementsApplication
{
    public static IServiceCollection AddAnnouncementsApplication(this IServiceCollection services)
    {
        var assembly = typeof(AnnouncementsApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AnnouncementsUnitOfWorkBehavior<,>));
        services.AddScoped<CourseAccess>();
        services.AddScoped<AnnouncementPresenter>();
        services.AddScoped<IAnnouncementsApi, AnnouncementsApi>();
        return services;
    }
}
