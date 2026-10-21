using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Audit.Application;

public static class AuditApplication
{
    public static IServiceCollection AddAuditApplication(this IServiceCollection services)
    {
        var assembly = typeof(AuditApplication).Assembly;
        services.AddRequestHandlers(assembly);
        services.AddValidators(assembly);
        services.AddIntegrationEventHandlers(assembly);   // AuditableActionPerformed → activity_logs
        return services;
    }
}
