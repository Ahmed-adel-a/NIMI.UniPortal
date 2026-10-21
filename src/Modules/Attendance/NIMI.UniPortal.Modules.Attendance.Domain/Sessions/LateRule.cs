namespace NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

/// <summary>
/// Rule R5: a check-in is late only when the university's wall clock is after the slot start plus the effective
/// grace. Exactly at the end of the grace period is still on time.
/// </summary>
public static class LateRule
{
    public static bool IsLate(
        DateTimeOffset checkIn,
        DateOnly sessionDate,
        TimeOnly slotStart,
        int graceMinutes,
        TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        var local = TimeZoneInfo.ConvertTime(checkIn, zone);
        var threshold = sessionDate.ToDateTime(slotStart, DateTimeKind.Unspecified).AddMinutes(graceMinutes);
        return local.DateTime > threshold;
    }
}
