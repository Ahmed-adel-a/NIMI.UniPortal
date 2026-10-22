using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

internal sealed class OverridePromotionHandler(
    ISemesterDbContext db, ISchedulingApi scheduling, PromotionService promotions, IEventCollector events, TimeProvider clock)
    : IRequestHandler<OverridePromotionCommand, PromotionDto>
{
    public async Task<PromotionDto> Handle(OverridePromotionCommand command, CancellationToken cancellationToken)
    {
        var standing = await db.Standings.SingleOrDefaultAsync(s => s.StudentId == command.StudentId, cancellationToken)
            ?? throw new NotFoundException("The student has not been placed in a year level.");
        if (standing.Status is StandingStatus.Graduated or StandingStatus.Withdrawn)
            throw new ConflictException("The student has already left the programme.");
        var finalLevel = await promotions.FinalLevelAsync(standing.DepartmentId, cancellationToken)
            ?? throw ValidationErrors.For("studentId", "The student's department has no study plan yet.");

        var movesUp = command.Result is PromotionResult.Promoted or PromotionResult.PromotedWithCarried;
        if (movesUp && standing.Level >= finalLevel)
            throw ValidationErrors.For("result", "The student is in the final level; choose graduated instead.");

        var now = clock.GetUtcNow();
        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        var academicYear = term?.AcademicYear ?? now.Year;
        var outcome = new PromotionOutcome(command.Result, movesUp ? standing.Level + 1 : standing.Level, command.Reason!.Trim());
        var failed = await promotions.FailedSubjectsAsync(command.StudentId, cancellationToken);
        var decision = promotions.Apply(standing, academicYear, outcome, failed, command.AdminId);

        events.Add(PromotionNotices.For(decision));
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), now, command.AdminId, AuditActions.PromotionOverridden, AuditEntities.User, command.StudentId,
            $"Year-end decision set to {command.Result} (level {decision.FromLevel} → {decision.ToLevel}): {outcome.Reason}"));

        return PromotionDto.From(decision);
    }
}
