using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

internal sealed class SavePromotionPolicyHandler(
    ISemesterDbContext db, IUsersApi users, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<SavePromotionPolicyCommand, PromotionPolicyDto>
{
    public async Task<PromotionPolicyDto> Handle(SavePromotionPolicyCommand command, CancellationToken cancellationToken)
    {
        if (command.FacultyId is { } facultyId && await users.GetFacultyAsync(facultyId, cancellationToken) is null)
            throw new NotFoundException($"No faculty with id '{facultyId}'.");

        var now = clock.GetUtcNow();
        var gates = command.GateLevels ?? [];
        var policy = await db.PromotionPolicies
            .SingleOrDefaultAsync(p => p.FacultyId == command.FacultyId, cancellationToken);
        if (policy is null)
        {
            policy = command.FacultyId is { } id
                ? PromotionPolicy.CreateForFaculty(id, command.MaxCarriedSubjects, gates, command.MaxYearsPerLevel, command.RequireFeesPaid, now)
                : PromotionPolicy.CreateDefault(command.MaxCarriedSubjects, gates, command.MaxYearsPerLevel, command.RequireFeesPaid, now);
            db.PromotionPolicies.Add(policy);
        }
        else
        {
            policy.Update(command.MaxCarriedSubjects, gates, command.MaxYearsPerLevel, command.RequireFeesPaid, now);
        }

        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), now, currentUser.UserId, AuditActions.PromotionPolicyUpdated, AuditEntities.PromotionPolicy, policy.Id,
            $"Promotion policy ({(command.FacultyId is null ? "university" : "faculty")}): {policy.MaxCarriedSubjects} carried, " +
            $"gates [{string.Join(", ", policy.GateLevels)}], {policy.MaxYearsPerLevel} years per level, fees {(policy.RequireFeesPaid ? "required" : "not required")}."));

        return new PromotionPolicyDto(
            command.FacultyId,
            command.FacultyId is null ? EffectivePolicies.UniversitySource : EffectivePolicies.FacultySource,
            policy.MaxCarriedSubjects,
            policy.GateLevels,
            policy.MaxYearsPerLevel,
            policy.RequireFeesPaid);
    }
}
