using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Schedule.GetStudentSchedule;

/// <summary>P4.5 — the student's own occurrences between two dates (same range rules as the teaching schedule).</summary>
public sealed record GetStudentScheduleQuery(
    Guid StudentId,
    bool IsStudent,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<StudentOccurrenceDto>>;
