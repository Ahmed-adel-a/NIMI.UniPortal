namespace Host.CairoUniversity.Dashboards;

/// <summary>
/// One Student Affairs metric as the catalog describes it. <see cref="Unit"/> is <c>count</c>, <c>percent</c> or
/// <c>currency</c>; <see cref="DrillDownUrl"/> is the existing paged list behind it (the client appends the same
/// filters), or null when there is none.
/// </summary>
public sealed record DashboardMetricDto(string Key, string Title, string TitleAr, string Unit, string? DrillDownUrl);
