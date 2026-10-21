using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Behaviors;
using NIMI.UniPortal.Modules.Payments.Application.Online;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application;

public static class PaymentsApplication
{
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        var assembly = typeof(PaymentsApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PaymentsUnitOfWorkBehavior<,>));
        services.AddScoped<IMockPaymentCompleter, MockPaymentCompleter>();
        services.AddIntegrationEventHandlers(assembly);   // MilitaryEducationRequested → student fee + notification
        return services;
    }
}
