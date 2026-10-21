using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.GetMyRegistrations;

internal sealed class GetMyRegistrationsHandler(
    ICreditHoursDbContext db, ISchedulingApi scheduling, IUsersApi users)
    : IRequestHandler<GetMyRegistrationsQuery, MyRegistrationsDto>
{
    public async Task<MyRegistrationsDto> Handle(
        GetMyRegistrationsQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsStudent)
            throw new ForbiddenException("Only students see their registrations.");

        var student = await users.GetUserAsync(query.StudentId, cancellationToken)
            ?? throw new NotFoundException("The signed-in student was not found.");

        var term = query.TermId is { } termId
            ? await scheduling.GetTermAsync(termId, cancellationToken)
                ?? throw new NotFoundException($"No term with id '{termId}'.")
            : await scheduling.GetCurrentTermAsync(cancellationToken)
                ?? throw new NotFoundException("There is no active term.");

        var window = await db.RegistrationWindows.AsNoTracking()
            .FirstOrDefaultAsync(w => w.TermId == term.Id, cancellationToken);

        var registrations = await db.CourseRegistrations.AsNoTracking()
            .Where(r => r.StudentId == student.Id &&
                        r.TermId == term.Id &&
                        r.Status != RegistrationStatus.Dropped)
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var offerings = await scheduling.GetOfferingsAsync(term.Id, cancellationToken);
        var offeringByGroup = offerings.ToDictionary(o => o.Group.Id);

        var items = new List<MyRegistrationDto>(registrations.Count);
        foreach (var registration in registrations)
        {
            if (offeringByGroup.TryGetValue(registration.SubjectGroupId, out var offering))
            {
                items.Add(new MyRegistrationDto(
                    registration.Id,
                    offering.Subject.Id,
                    offering.Subject.Code,
                    offering.Subject.Name,
                    offering.Subject.NameAr,
                    offering.Group.Id,
                    offering.Group.GroupNumber,
                    registration.CreditHours,
                    registration.Status,
                    registration.RegisteredAt));
                continue;
            }

            // A group that was deactivated after registering still shows; resolve it through the contract.
            var group = await scheduling.GetGroupAsync(registration.SubjectGroupId, cancellationToken);
            var subject = group is null
                ? null
                : await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken);
            if (group is null || subject is null)
                continue;

            items.Add(new MyRegistrationDto(
                registration.Id,
                subject.Id,
                subject.Code,
                subject.Name,
                subject.NameAr,
                group.Id,
                group.GroupNumber,
                registration.CreditHours,
                registration.Status,
                registration.RegisteredAt));
        }

        var ordered = items
            .OrderBy(i => i.SubjectCode, StringComparer.Ordinal)
            .ThenBy(i => i.GroupNumber)
            .ToList();
        var totalCredits = registrations.Sum(r => r.CreditHours);

        return new MyRegistrationsDto(
            term.Id,
            ordered,
            totalCredits,
            window?.MinCredits,
            window?.MaxCredits,
            window is not null && totalCredits < window.MinCredits);
    }
}
