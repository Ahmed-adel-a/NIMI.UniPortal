using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms;

/// <summary>A term as the HTTP API shows it; enums serialize as snake_case text.</summary>
public sealed record TermDto(
    Guid Id,
    int AcademicYear,
    TermNumber Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount,
    TermStatus Status)
{
    public static TermDto From(Term term) =>
        new(term.Id, term.AcademicYear, term.Number, term.StartsOn, term.EndsOn, term.WeekCount, term.Status);
}
