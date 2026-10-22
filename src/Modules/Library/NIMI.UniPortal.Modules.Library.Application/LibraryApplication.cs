using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Library.Application.Behaviors;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Application.Digital;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application;

public static class LibraryApplication
{
    public static IServiceCollection AddLibraryApplication(this IServiceCollection services)
    {
        var assembly = typeof(LibraryApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LibraryUnitOfWorkBehavior<,>));
        services.AddScoped<LoanPolicies>();
        services.AddScoped<HoldQueue>();
        services.AddScoped<LibraryNotices>();
        services.AddScoped<LoanPresenter>();
        services.AddScoped<DigitalScope>();   // who a digital book is for (DIGILIB)
        return services;
    }
}
