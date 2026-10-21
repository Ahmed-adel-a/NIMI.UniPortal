namespace NIMI.UniPortal.Modules.Attendance.Domain.Records;

/// <summary>Rule R9: attended ÷ held, two decimals away from zero; a group with no closed session is at 100 %.</summary>
public static class AttendanceRate
{
    public static decimal Percentage(int attended, int held) =>
        held == 0
            ? 100m
            : Math.Round(attended * 100m / held, 2, MidpointRounding.AwayFromZero);
}
