namespace NIMI.UniPortal.Modules.Scheduling.Contracts;

/// <summary>A term as other modules see it; <see cref="Number"/> and <see cref="Status"/> are snake_case text.</summary>
public sealed record TermRef(
    Guid Id,
    int AcademicYear,
    string Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount,
    string Status);
