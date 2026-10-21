using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.GetOfferings;

internal sealed class GetOfferingsHandler(
    ICreditHoursDbContext db,
    ISchedulingApi scheduling,
    IUsersApi users,
    TimeProvider clock)
    : IRequestHandler<GetOfferingsQuery, PagedResult<OfferingDto>>
{
    private const string MilitaryEducation = "military_education";
    private const string GroupFullMessage = "The group is full.";

    public async Task<PagedResult<OfferingDto>> Handle(
        GetOfferingsQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsStudent)
            throw new ForbiddenException("Only students see registration offerings.");

        var student = await users.GetUserAsync(query.StudentId, cancellationToken)
            ?? throw new NotFoundException("The signed-in student was not found.");

        var term = query.TermId is { } termId
            ? await scheduling.GetTermAsync(termId, cancellationToken)
                ?? throw new NotFoundException($"No term with id '{termId}'.")
            : await scheduling.GetCurrentTermAsync(cancellationToken)
                ?? throw new NotFoundException("There is no active term.");

        var offerings = await scheduling.GetOfferingsAsync(term.Id, cancellationToken);
        var window = await db.RegistrationWindows.AsNoTracking()
            .FirstOrDefaultAsync(w => w.TermId == term.Id, cancellationToken);

        var registrations = await db.CourseRegistrations.AsNoTracking()
            .Where(r => r.StudentId == student.Id &&
                        r.TermId == term.Id &&
                        r.Status != RegistrationStatus.Dropped)
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var currentCredits = registrations.Sum(r => r.CreditHours);
        var registeredGroupIds = registrations.Select(r => r.SubjectGroupId).ToList();
        var registeredSubjectIds = registrations.Select(r => r.SubjectId).ToHashSet();

        var groupIds = offerings.Select(o => o.Group.Id).ToList();
        var seats = await db.GroupSeats.AsNoTracking()
            .Where(s => groupIds.Contains(s.SubjectGroupId))
            .ToDictionaryAsync(s => s.SubjectGroupId, s => s.Taken, cancellationToken);

        var subjectIds = offerings.Select(o => o.Subject.Id).ToList();
        var prerequisiteRows = new List<SubjectPrerequisite>();
        if (subjectIds.Count > 0)
            prerequisiteRows = await db.SubjectPrerequisites.AsNoTracking()
                .Where(p => subjectIds.Contains(p.SubjectId))
                .ToListAsync(cancellationToken);

        var prerequisiteIdsBySubject = prerequisiteRows
            .GroupBy(p => p.SubjectId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.PrerequisiteSubjectId).ToList());

        var passedSubjectIds = (await db.CreditLedger.AsNoTracking()
            .Where(e => e.StudentId == student.Id && e.Passed)
            .Select(e => e.SubjectId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var offeringByGroup = offerings.ToDictionary(o => o.Group.Id);
        var codeBySubject = new Dictionary<Guid, string>();

        async Task<string?> CodeOfAsync(Guid subjectId)
        {
            if (codeBySubject.TryGetValue(subjectId, out var cached))
                return cached;

            var subject = await scheduling.GetSubjectAsync(subjectId, cancellationToken);
            if (subject is null)
                return null;

            codeBySubject[subjectId] = subject.Code;
            return subject.Code;
        }

        async Task<string?> GroupNameAsync(Guid groupId)
        {
            if (offeringByGroup.TryGetValue(groupId, out var offered))
                return $"{offered.Subject.Code} group {offered.Group.GroupNumber}";

            var group = await scheduling.GetGroupAsync(groupId, cancellationToken);
            if (group is null)
                return null;

            var code = await CodeOfAsync(group.SubjectId);
            return code is null ? null : $"{code} group {group.GroupNumber}";
        }

        var now = clock.GetUtcNow();
        var items = new List<OfferingDto>(offerings.Count);

        foreach (var offering in offerings)
        {
            var seatsLeft = Math.Max(
                0, offering.Group.Capacity - seats.GetValueOrDefault(offering.Group.Id));

            var missingPrerequisiteCodes = new List<string>();
            if (prerequisiteIdsBySubject.TryGetValue(offering.Subject.Id, out var prerequisiteIds))
            {
                foreach (var prerequisiteId in prerequisiteIds)
                {
                    if (passedSubjectIds.Contains(prerequisiteId))
                        continue;

                    var code = await CodeOfAsync(prerequisiteId);
                    if (code is not null)
                        missingPrerequisiteCodes.Add(code);
                }

                missingPrerequisiteCodes.Sort(StringComparer.Ordinal);
            }

            var clashingGroups = new List<string>();
            var otherGroupIds = registeredGroupIds.Where(id => id != offering.Group.Id).ToList();
            if (otherGroupIds.Count > 0)
            {
                var conflicts = await scheduling.FindTimeConflictsAsync(
                    offering.Group.Id, otherGroupIds, cancellationToken);

                foreach (var conflictId in conflicts)
                {
                    var name = await GroupNameAsync(conflictId);
                    if (name is not null)
                        clashingGroups.Add(name);
                }
            }

            var context = new RegistrationContext(
                Now: now,
                Window: window,
                GroupIsOffered: true,
                AlreadyRegisteredForSubject: registeredSubjectIds.Contains(offering.Subject.Id),
                IsMilitaryEducation: offering.Subject.Category == MilitaryEducation,
                TargetDepartmentId: offering.Group.TargetDepartmentId,
                StudentDepartmentId: student.DepartmentId,
                MissingPrerequisiteCodes: missingPrerequisiteCodes,
                CurrentCredits: currentCredits,
                SubjectCredits: offering.Subject.CreditHours,
                ClashingGroups: clashingGroups);

            var reasons = RegistrationPolicy.Check(context).ToList();
            if (seatsLeft == 0)
                reasons.Add(GroupFullMessage);

            var slots = offering.Slots
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ThenBy(s => s.Id)
                .Select(s => new OfferingSlotDto(s.DayOfWeek, s.StartTime, s.EndTime, s.FacilityName))
                .ToList();

            items.Add(new OfferingDto(
                GroupId: offering.Group.Id,
                GroupNumber: offering.Group.GroupNumber,
                SubjectId: offering.Subject.Id,
                SubjectCode: offering.Subject.Code,
                SubjectName: offering.Subject.Name,
                SubjectNameAr: offering.Subject.NameAr,
                CreditHours: offering.Subject.CreditHours,
                Capacity: offering.Group.Capacity,
                SeatsLeft: seatsLeft,
                Slots: slots,
                Eligible: reasons.Count == 0,
                Reasons: reasons));
        }

        return new PagedResult<OfferingDto>(
            items.Skip(query.Paging.Skip).Take(query.Paging.PageSize).ToList(),
            query.Paging.Page,
            query.Paging.PageSize,
            items.Count);
    }
}
