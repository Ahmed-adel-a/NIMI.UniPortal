using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.DeleteTeachingAssignment;

internal sealed class DeleteTeachingAssignmentHandler(ISchedulingDbContext db, TimeProvider clock)
    : IRequestHandler<DeleteTeachingAssignmentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteTeachingAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await db.TeachingAssignments
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No teaching assignment with id '{command.Id}'.");

        assignment.Deactivate(clock.GetUtcNow());
        return Unit.Value;
    }
}
