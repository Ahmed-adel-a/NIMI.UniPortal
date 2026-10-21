namespace Host.CairoUniversity.Dashboards;

/// <summary>
/// P10.6 — every catalog metric's value for the filter (term defaults to the active one). A metric whose source failed
/// is null and named in <see cref="Unavailable"/>; a term-bound metric is null when there is no term.
/// </summary>
public sealed record StudentAffairsSummaryDto(
    Guid? TermId,
    Guid? FacultyId,
    Guid? DepartmentId,
    IReadOnlyDictionary<string, decimal?> Values,
    IReadOnlyList<string> Unavailable,
    DateTimeOffset GeneratedAt);
