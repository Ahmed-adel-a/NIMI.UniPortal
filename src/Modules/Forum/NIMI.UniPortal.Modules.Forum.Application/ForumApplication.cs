using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Forum.Application.Behaviors;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application;

public static class ForumApplication
{
    public static IServiceCollection AddForumApplication(this IServiceCollection services)
    {
        var assembly = typeof(ForumApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ForumUnitOfWorkBehavior<,>));
        services.AddScoped<ForumAccess>();
        services.AddScoped<AuthorDirectory>();
        services.AddScoped<VisiblePosts>();
        return services;
    }
}
