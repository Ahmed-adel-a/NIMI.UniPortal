namespace NIMI.UniPortal.Modules.Scheduling.Contracts;

/// <summary>
/// What other modules may read from Scheduling (P3). Read-only DTOs only: no entities, no <c>IQueryable</c>,
/// no cross-schema joins. Consumers (P4 registration, lifecycle, attendance) get timetable logic from here instead
/// of re-implementing it.
/// </summary>
public interface ISchedulingApi
{
    /// <summary>The term currently marked active, or null when none is (rule R1).</summary>
    Task<TermRef?> GetCurrentTermAsync(CancellationToken cancellationToken = default);

    Task<TermRef?> GetTermAsync(Guid termId, CancellationToken cancellationToken = default);

    Task<SubjectRef?> GetSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<GroupRef?> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Active groups of a term with their subject and non-cancelled slots (P4 registration).</summary>
    Task<IReadOnlyList<OfferingRef>> GetOfferingsAsync(Guid termId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlotRef>> GetSlotsForGroupsAsync(
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default);

    Task<SlotRef?> GetSlotAsync(Guid slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// An active assignment for (staff, subject, term) whose group is null (the whole subject) or exactly
    /// <paramref name="groupId"/> (rule R5).
    /// </summary>
    Task<bool> IsTeachingAsync(
        Guid staffId,
        Guid subjectId,
        Guid termId,
        Guid? groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of the active groups this staff member teaches in a term: assignment rows naming a group, plus every
    /// active group of a subject they are assigned to without a group.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetGroupIdsTaughtByAsync(
        Guid staffId,
        Guid termId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Real occurrences of the groups' active slots in a date range (P4 attendance/rosters), built with
    /// <c>ScheduleSlot.OccurrencesBetween</c>; cancelled slots are ignored.
    /// </summary>
    Task<IReadOnlyList<OccurrenceRef>> GetOccurrencesAsync(
        IReadOnlyCollection<Guid> groupIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of <paramref name="otherGroupIds"/> whose active slots clash in time and date with the candidate group's
    /// active slots (rule R8; the facility/staff part of <c>ConflictsWith</c> is irrelevant for a student's clash).
    /// </summary>
    Task<IReadOnlyList<Guid>> FindTimeConflictsAsync(
        Guid candidateGroupId,
        IReadOnlyCollection<Guid> otherGroupIds,
        CancellationToken cancellationToken = default);

    /// <summary>Active subjects flagged as graduation requirements (P4 graduation checks), by code.</summary>
    Task<IReadOnlyList<SubjectRef>> GetGraduationRequirementSubjectsAsync(CancellationToken cancellationToken = default);
}
