using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of the copy-term endpoint: the new term's own calendar (what to carry over comes from the source term).</summary>
public sealed record CopyTermRequest(
    int AcademicYear,
    TermNumber Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount);
