using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Behaviors;

/// <summary>Commits Grades commands — and only Grades commands — with exactly one SaveChangesAsync.</summary>
internal sealed class GradesUnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider services)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>(services)
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => typeof(GradesApplication).Assembly;

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Resolved lazily: other modules' requests pass through without ever creating a GradesDbContext.
        var db = Services.GetRequiredService<IGradesDbContext>();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("These grades were changed by someone else since you loaded them. Reload and try again.");
        }
    }
}
