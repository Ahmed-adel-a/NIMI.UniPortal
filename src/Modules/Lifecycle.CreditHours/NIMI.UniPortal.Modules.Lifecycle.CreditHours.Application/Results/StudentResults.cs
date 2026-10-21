using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Results;
using NIMI.UniPortal.Modules.Scheduling.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Results;

/// <summary>
/// P9.3b — rebuilds a student's GPA snapshots (one per term with ledger rows) from the saved credit ledger. Every
/// snapshot is recomputed, because a correction in an early term changes the cumulative GPA of every later term.
/// </summary>
internal sealed class StudentResults(ICreditHoursDbContext db, ISchedulingApi scheduling)
{
    public async Task RecomputeAsync(Guid studentId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var ledger = await db.CreditLedger.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);

        var starts = new Dictionary<Guid, DateOnly>();
        foreach (var termId in ledger.Select(e => e.TermId).Distinct())
        {
            if (await scheduling.GetTermAsync(termId, cancellationToken) is { } term)
                starts[termId] = term.StartsOn;
        }

        var lines = ledger
            .Where(e => starts.ContainsKey(e.TermId))
            .Select(e => new ResultLine(e.SubjectId, e.TermId, starts[e.TermId], e.CreditHours, e.GradePoints ?? 0m, e.Passed))
            .ToList();

        var snapshots = await db.GpaSnapshots
            .Where(s => s.StudentId == studentId)
            .ToDictionaryAsync(s => s.TermId, cancellationToken);

        foreach (var (termId, startsOn) in starts)
        {
            if (!snapshots.TryGetValue(termId, out var snapshot))
            {
                snapshot = GpaSnapshot.Create(studentId, termId, startsOn);
                db.GpaSnapshots.Add(snapshot);
            }

            snapshot.Recompute(lines, now);
        }
    }
}
