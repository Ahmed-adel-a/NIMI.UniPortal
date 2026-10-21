using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/terms/{id}</c>.</summary>
public sealed record UpdateTermRequest(
    int AcademicYear,
    TermNumber Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount);
