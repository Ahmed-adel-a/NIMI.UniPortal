using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CopyTerm;

/// <summary>
/// Creates the planned term, then carries over the source term's active groups, teaching assignments and (via
/// <see cref="SlotCopier"/>) timetable slots. Anything that no longer fits is skipped and reported, never an error:
/// the copy is a draft the admin reviews. No notifications go out and nothing students own is touched.
/// </summary>
internal sealed class CopyTermHandler(
    ISchedulingDbContext db,
    IUsersApi users,
    SlotCopier slotCopier,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<CopyTermCommand, CopyTermResultDto>
{
    public async Task<CopyTermResultDto> Handle(CopyTermCommand command, CancellationToken cancellationToken)
    {
        var source = await db.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.Id == command.SourceTermId, cancellationToken)
            ?? throw new NotFoundException($"No term with id '{command.SourceTermId}'.");

        var now = clock.GetUtcNow();
        var term = Term.Create(command.AcademicYear, command.Number, command.StartsOn, command.EndsOn, command.WeekCount, now);

        var duplicate = await db.Terms.AnyAsync(
            t => t.AcademicYear == term.AcademicYear && t.Number == term.Number, cancellationToken);
        if (duplicate)
            throw new ConflictException(
                $"A {term.Number.ToString().ToLowerInvariant()} term for {term.AcademicYear} already exists.");

        db.Terms.Add(term);

        var skipped = new List<SkippedItemDto>();
        var groups = await CopyGroupsAsync(source, term, now, skipped, cancellationToken);
        var assignments = await CopyAssignmentsAsync(source, term, groups, command.CreatedBy, now, skipped, cancellationToken);
        var slots = await slotCopier.CopyAsync(source, term, groups, command.CreatedBy, now, skipped, cancellationToken);

        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            now,
            command.CreatedBy,
            AuditActions.TermCopied,
            AuditEntities.Term,
            term.Id,
            $"{term.Number} {term.AcademicYear} created from {source.Number} {source.AcademicYear}: " +
            $"{groups.Count} groups, {assignments} assignments, {slots} slots copied; {skipped.Count} skipped."));

        return new CopyTermResultDto(TermDto.From(term), new CopiedCountsDto(groups.Count, assignments, slots), skipped);
    }

    private async Task<Dictionary<Guid, CopiedGroup>> CopyGroupsAsync(
        Term source, Term term, DateTimeOffset now, List<SkippedItemDto> skipped, CancellationToken cancellationToken)
    {
        var sourceGroups = await db.SubjectGroups.AsNoTracking()
            .Include(g => g.Subject)
            .Where(g => g.TermId == source.Id && g.IsActive)
            .OrderBy(g => g.Subject.Code).ThenBy(g => g.GroupNumber)
            .ToListAsync(cancellationToken);

        var copied = new Dictionary<Guid, CopiedGroup>();
        foreach (var sourceGroup in sourceGroups)
        {
            if (!sourceGroup.Subject.IsActive)
            {
                skipped.Add(new SkippedItemDto(
                    SkippedItemKind.Group, sourceGroup.Subject.Code, sourceGroup.GroupNumber, SkipReason.SubjectInactive));
                continue;
            }

            var group = SubjectGroup.Create(
                sourceGroup.Subject, term, sourceGroup.GroupNumber, sourceGroup.TargetDepartmentId, sourceGroup.Capacity, now);
            db.SubjectGroups.Add(group);
            copied[sourceGroup.Id] = new CopiedGroup(group, sourceGroup.Subject.Code, sourceGroup.Subject.DepartmentId);
        }

        return copied;
    }

    private async Task<int> CopyAssignmentsAsync(
        Term source,
        Term term,
        Dictionary<Guid, CopiedGroup> groups,
        Guid createdBy,
        DateTimeOffset now,
        List<SkippedItemDto> skipped,
        CancellationToken cancellationToken)
    {
        var sourceAssignments = await db.TeachingAssignments.AsNoTracking()
            .Include(a => a.Subject)
            .Where(a => a.TermId == source.Id && a.IsActive)
            .OrderBy(a => a.Subject.Code)
            .ToListAsync(cancellationToken);
        if (sourceAssignments.Count == 0)
            return 0;

        var staff = (await users.GetUsersAsync(sourceAssignments.Select(a => a.StaffId).Distinct().ToList(), cancellationToken))
            .ToDictionary(u => u.Id);

        var copied = 0;
        foreach (var sourceAssignment in sourceAssignments)
        {
            CopiedGroup? group = null;
            if (sourceAssignment.SubjectGroupId is { } sourceGroupId)
            {
                // A group that was not copied has already been reported; its assignments go with it.
                if (!groups.TryGetValue(sourceGroupId, out group))
                    continue;
            }
            else if (!sourceAssignment.Subject.IsActive)
            {
                skipped.Add(new SkippedItemDto(
                    SkippedItemKind.TeachingAssignment, sourceAssignment.Subject.Code, null, SkipReason.SubjectInactive));
                continue;
            }

            if (!staff.TryGetValue(sourceAssignment.StaffId, out var member) || !member.IsActive || !PortalRoles.CanTeach(member.Role))
            {
                skipped.Add(new SkippedItemDto(
                    SkippedItemKind.TeachingAssignment, sourceAssignment.Subject.Code, group?.Group.GroupNumber, SkipReason.StaffInactive));
                continue;
            }

            db.TeachingAssignments.Add(TeachingAssignment.Create(
                sourceAssignment.StaffId, sourceAssignment.Subject, term, group?.Group, sourceAssignment.Role, createdBy, now));
            copied++;
        }

        return copied;
    }
}
