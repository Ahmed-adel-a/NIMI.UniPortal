using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Behaviors;

/// <summary>Commits Payments commands — and only Payments commands — with exactly one SaveChangesAsync.</summary>
internal sealed class PaymentsUnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider services)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>(services)
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => typeof(PaymentsApplication).Assembly;

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Resolved lazily: other modules' requests pass through without ever creating a PaymentsDbContext.
        var db = Services.GetRequiredService<IPaymentsDbContext>();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This fee was changed by someone else since you loaded it. Reload and try again.");
        }
    }
}
