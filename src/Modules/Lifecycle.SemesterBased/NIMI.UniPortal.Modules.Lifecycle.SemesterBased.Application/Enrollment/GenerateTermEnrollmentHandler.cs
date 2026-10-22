using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Results;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

internal sealed class GenerateTermEnrollmentHandler(
    ISemesterDbContext db, ISchedulingApi scheduling, IUsersApi users, TimeProvider clock)
    : IRequestHandler<GenerateTermEnrollmentCommand, GenerationReportDto>
{
    public async Task<GenerationReportDto> Handle(GenerateTermEnrollmentCommand command, CancellationToken cancellationToken)
    {
        var term = await scheduling.GetTermAsync(command.TermId, cancellationToken)
            ?? throw new NotFoundException($"No term with id '{command.TermId}'.");
        var planTerm = term.Number switch
        {
            "first" => PlanTerm.First,
            "second" => PlanTerm.Second,
            _ => throw ValidationErrors.For("termId", "Enrollment is generated for first and second terms only."),
        };
        if (term.Status == "closed")
            throw new ConflictException("The term is closed.");
        if (command.DepartmentId is { } departmentId && await users.GetDepartmentAsync(departmentId, cancellationToken) is null)
            throw new NotFoundException($"No department with id '{departmentId}'.");

        var now = clock.GetUtcNow();
        var plans = await db.StudyPlans.AsNoTracking().Include(p => p.Subjects).ToDictionaryAsync(p => p.DepartmentId, cancellationToken);
        var offerings = (await scheduling.GetOfferingsAsync(term.Id, cancellationToken))
            .Where(o => o.Group.IsActive)
            .ToLookup(o => o.Subject.Id);

        var existing = await db.TermEnrollments
            .Where(e => e.TermId == term.Id && e.Status != EnrollmentStatus.Withdrawn)
            .Select(e => new { e.StudentId, e.SubjectId, e.SubjectGroupId })
            .ToListAsync(cancellationToken);
        var taken = existing.Select(e => (e.StudentId, e.SubjectId)).ToHashSet();
        var seats = existing.GroupBy(e => e.SubjectGroupId).ToDictionary(g => g.Key, g => g.Count());

        var studentIds = await users.GetActiveStudentIdsAsync(null, command.DepartmentId, cancellationToken);
        var standings = await db.Standings
            .Where(s => studentIds.Contains(s.StudentId))
            .ToDictionaryAsync(s => s.StudentId, cancellationToken);
        var owed = await StudentRecord.OutstandingAsync(db, studentIds, cancellationToken);

        var unplaced = new List<UnplacedDto>();
        int enrolled = 0, alreadyEnrolled = 0, newStandings = 0;
        foreach (var studentId in studentIds)
        {
            if (!standings.TryGetValue(studentId, out var standing))
            {
                var placement = await users.GetStudentPlacementAsync(studentId, cancellationToken);
                if (placement is null)
                    continue;
                standing = StudentStanding.Place(studentId, placement.DepartmentId, 1, now);
                db.Standings.Add(standing);
                newStandings++;
            }

            if (!standing.IsEnrolling)
            {
                if (standing.Status == StandingStatus.OnHold)
                    unplaced.Add(new UnplacedDto(studentId, null, "The year-end decision is on hold until the fees are paid."));
                continue;
            }

            if (!plans.TryGetValue(standing.DepartmentId, out var plan))
            {
                unplaced.Add(new UnplacedDto(studentId, null, "The student's department has no study plan yet."));
                continue;
            }

            // Plan subjects of the level (unless only failed subjects are taken again) + owed subjects taught this term.
            var subjects = new List<(Guid SubjectId, int Level, bool IsCarried)>();
            if (!standing.TakesFailedSubjectsOnly)
                subjects.AddRange(plan.SubjectsFor(standing.Level, planTerm).Select(s => (s.SubjectId, s.Level, false)));
            foreach (var subject in owed[studentId])
            {
                if (plan.Find(subject.SubjectId) is { } entry && entry.Term == planTerm && subjects.All(s => s.SubjectId != subject.SubjectId))
                    subjects.Add((subject.SubjectId, subject.Level, true));
            }

            foreach (var (subjectId, level, isCarried) in subjects)
            {
                if (taken.Contains((studentId, subjectId)))
                {
                    alreadyEnrolled++;
                    continue;
                }

                var groups = offerings[subjectId]
                    .Where(o => o.Group.TargetDepartmentId is null || o.Group.TargetDepartmentId == standing.DepartmentId)
                    .ToList();
                if (groups.Count == 0)
                {
                    unplaced.Add(new UnplacedDto(studentId, subjectId, "The subject has no group this term."));
                    continue;
                }

                // Balance: the group with the fewest students that still has a seat (lowest group number on a tie).
                var group = groups
                    .Where(o => seats.GetValueOrDefault(o.Group.Id) < o.Group.Capacity)
                    .OrderBy(o => seats.GetValueOrDefault(o.Group.Id))
                    .ThenBy(o => o.Group.GroupNumber)
                    .FirstOrDefault();
                if (group is null)
                {
                    unplaced.Add(new UnplacedDto(studentId, subjectId, "Every group of the subject is full."));
                    continue;
                }

                db.TermEnrollments.Add(TermEnrollment.Create(
                    studentId, term.Id, subjectId, group.Group.Id, level, group.Subject.CreditHours, isCarried, now));
                seats[group.Group.Id] = seats.GetValueOrDefault(group.Group.Id) + 1;
                taken.Add((studentId, subjectId));
                enrolled++;
            }
        }

        return new GenerationReportDto(enrolled, alreadyEnrolled, newStandings, unplaced);
    }
}
