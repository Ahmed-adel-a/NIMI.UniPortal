using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of the term create/update endpoints (P3.1).</summary>
public sealed record CreateTermRequest(
    int AcademicYear,
    TermNumber Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount);
