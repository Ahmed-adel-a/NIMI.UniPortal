using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Behaviors;

/// <summary>Commits Platform commands — and only Platform commands — with exactly one SaveChangesAsync.</summary>
internal sealed class PlatformUnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider services)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => typeof(PlatformApplication).Assembly;

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Resolved lazily: other modules' requests pass through without ever creating a PlatformDbContext.
        var db = services.GetRequiredService<IPlatformDbContext>();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The settings were changed by someone else since you loaded them. Reload and try again.");
        }
    }
}
