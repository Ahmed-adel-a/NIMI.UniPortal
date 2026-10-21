using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Behaviors;

/// <summary>Commits Assignments commands — and only Assignments commands — with exactly one SaveChangesAsync.</summary>
internal sealed class AssignmentsUnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider services)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>(services)
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => typeof(AssignmentsApplication).Assembly;

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Resolved lazily: other modules' requests pass through without ever creating a AssignmentsDbContext.
        var db = Services.GetRequiredService<IAssignmentsDbContext>();
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
