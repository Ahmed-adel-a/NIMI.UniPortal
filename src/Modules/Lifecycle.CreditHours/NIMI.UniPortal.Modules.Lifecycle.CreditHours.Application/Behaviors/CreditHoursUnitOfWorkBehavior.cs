using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Behaviors;

/// <summary>Commits CreditHours commands — and only this module's commands — with exactly one SaveChangesAsync.</summary>
internal sealed class CreditHoursUnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider services)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>(services)
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => typeof(CreditHoursApplication).Assembly;

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Resolved lazily: other modules' requests pass through without ever creating a CreditHoursDbContext.
        var db = Services.GetRequiredService<ICreditHoursDbContext>();
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
