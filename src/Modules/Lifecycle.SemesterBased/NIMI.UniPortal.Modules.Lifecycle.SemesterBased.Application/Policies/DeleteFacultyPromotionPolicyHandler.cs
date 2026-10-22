using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

internal sealed class DeleteFacultyPromotionPolicyHandler(
    ISemesterDbContext db, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<DeleteFacultyPromotionPolicyCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFacultyPromotionPolicyCommand command, CancellationToken cancellationToken)
    {
        var policy = await db.PromotionPolicies
            .SingleOrDefaultAsync(p => p.FacultyId == command.FacultyId, cancellationToken)
            ?? throw new NotFoundException("This faculty has no promotion policy of its own.");

        db.PromotionPolicies.Remove(policy);
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), clock.GetUtcNow(), currentUser.UserId, AuditActions.PromotionPolicyRemoved,
            AuditEntities.PromotionPolicy, policy.Id, "Faculty promotion policy removed; the university policy applies again."));
        return Unit.Value;
    }
}
