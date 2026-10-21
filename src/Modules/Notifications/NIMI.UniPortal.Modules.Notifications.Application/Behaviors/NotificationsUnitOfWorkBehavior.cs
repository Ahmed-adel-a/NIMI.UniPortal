using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Behaviors;

/// <summary>Commits Notifications commands — and only Notifications commands — with exactly one SaveChangesAsync.</summary>
internal sealed class NotificationsUnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider services)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>(services)
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => typeof(NotificationsApplication).Assembly;

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Resolved lazily: other modules' requests pass through without ever creating a NotificationsDbContext.
        var db = Services.GetRequiredService<INotificationsDbContext>();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This record was changed by someone else since you loaded it. Reload and try again.");
        }
    }
}
