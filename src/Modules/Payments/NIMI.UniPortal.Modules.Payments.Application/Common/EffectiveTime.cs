namespace NIMI.UniPortal.Modules.Payments.Application.Common;

/// <summary>
/// Wall-clock helpers for the effective faculty timezone (SETTINGS). The clock itself stays UTC; only the
/// student-facing "today" and the receipt year are interpreted here.
/// </summary>
internal static class EffectiveTime
{
    public static DateOnly Today(DateTimeOffset now, string? timeZone) =>
        DateOnly.FromDateTime(Convert(now, timeZone));

    public static int Year(DateTimeOffset now, string? timeZone) =>
        Convert(now, timeZone).Year;

    private static DateTime Convert(DateTimeOffset now, string? timeZone)
    {
        var utc = now.UtcDateTime;
        return !string.IsNullOrWhiteSpace(timeZone)
               && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var zone)
            ? TimeZoneInfo.ConvertTimeFromUtc(utc, zone)
            : utc;
    }
}
