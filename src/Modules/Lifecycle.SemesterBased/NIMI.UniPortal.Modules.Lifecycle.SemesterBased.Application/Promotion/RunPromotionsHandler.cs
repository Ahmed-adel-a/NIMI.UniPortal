using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

internal sealed class RunPromotionsHandler(
    ISemesterDbContext db, ISchedulingApi scheduling, PromotionService promotions, IEventCollector events)
    : IRequestHandler<RunPromotionsCommand, RunPromotionsDto>
{
    public async Task<RunPromotionsDto> Handle(RunPromotionsCommand command, CancellationToken cancellationToken)
    {
        var term = await scheduling.GetTermAsync(command.TermId, cancellationToken)
            ?? throw new NotFoundException($"No term with id '{command.TermId}'.");
        if (term.Number != "second")
            throw ValidationErrors.For("termId", "Promotion is decided after the second term of the academic year.");

        var decisions = new List<YearPromotion>();
        foreach (var standing in await promotions.ReadyForDecisionAsync(term.Id, term.AcademicYear, null, cancellationToken))
        {
            if (await promotions.DecideAsync(standing, term.AcademicYear, cancellationToken) is { } decision)
                decisions.Add(decision);
        }

        // Held students of this year are decided again (their fees may be paid by now); still unpaid stays held.
        var held = await db.Standings.Where(s => s.Status == StandingStatus.OnHold).ToListAsync(cancellationToken);
        foreach (var standing in held)
        {
            var heldThisYear = await db.YearPromotions.AnyAsync(
                p => p.StudentId == standing.StudentId && p.AcademicYear == term.AcademicYear && p.Result == PromotionResult.Held,
                cancellationToken);
            if (heldThisYear && await promotions.DecideAgainAsync(standing, term.AcademicYear, cancellationToken) is { } decision)
                decisions.Add(decision);
        }

        foreach (var decision in decisions)
            events.Add(PromotionNotices.For(decision));

        var waiting = await db.TermEnrollments
            .Where(e => e.TermId == term.Id && e.Status == EnrollmentStatus.Enrolled)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);
        return new RunPromotionsDto(
            decisions.Count(d => d.Result != PromotionResult.Held), decisions.Count(d => d.Result == PromotionResult.Held), waiting);
    }
}
