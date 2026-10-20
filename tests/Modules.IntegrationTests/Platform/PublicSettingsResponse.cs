namespace NIMI.UniPortal.Modules.IntegrationTests.Platform;

/// <summary>
/// The JSON shape of GET /api/platform/settings, as the frontend sees it. Declared independently of the
/// API's DTO on purpose: a rename on the server side must break this test, not silently follow it.
/// </summary>
internal sealed record PublicSettingsResponse(
    string UniversityName,
    string UniversityNameAr,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string TimeZone,
    int WeekStartDay,
    int[] WorkingDays);
