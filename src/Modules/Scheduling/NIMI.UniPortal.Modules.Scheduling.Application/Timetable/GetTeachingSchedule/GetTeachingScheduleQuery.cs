using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.GetTeachingSchedule;

/// <summary>
/// A professor's own occurrences (P3.6). <paramref name="IsProfessor"/> is derived from the caller's claims so the
/// handler can enforce rule R10 without depending on HTTP.
/// </summary>
public sealed record GetTeachingScheduleQuery(
    Guid StaffId,
    bool IsProfessor,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<TeachingOccurrenceDto>>;
