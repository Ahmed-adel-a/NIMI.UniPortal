// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

namespace NIMI.UniPortal.Modules.UnitTests.Attendance;

/// <summary>Rule R5 — late after slot start + grace, measured on the university's wall clock.</summary>
public sealed class LateRuleTests
{
    private static readonly TimeZoneInfo Cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
    private static readonly DateOnly Sunday = new(2026, 10, 25);
    private static readonly TimeOnly NineAm = new(9, 0);

    [Theory]
    [InlineData(9, 0, false)]
    [InlineData(9, 10, false)]   // exactly at the end of the grace period is still on time
    [InlineData(9, 11, true)]
    [InlineData(8, 55, false)]   // early is fine
    public void Lateness_is_judged_on_the_local_wall_clock(int hour, int minute, bool late)
    {
        var localCheckIn = new DateTime(2026, 10, 25, hour, minute, 0, DateTimeKind.Unspecified);
        var checkIn = new DateTimeOffset(localCheckIn, Cairo.GetUtcOffset(localCheckIn));

        Assert.Equal(late, LateRule.IsLate(checkIn, Sunday, NineAm, 10, Cairo));
    }

    [Fact]
    public void The_same_instant_can_be_on_time_in_one_zone_and_late_in_another()
    {
        var plusThree = TimeZoneInfo.CreateCustomTimeZone("Test+03", TimeSpan.FromHours(3), "Test+03", "Test+03");
        var checkIn = new DateTimeOffset(2026, 10, 25, 7, 5, 0, TimeSpan.Zero);                      // 07:05 UTC = 10:05 at +03

        Assert.True(LateRule.IsLate(checkIn, Sunday, NineAm, 10, TimeZoneInfo.Utc) == false);         // 07:05 UTC is before 09:00 UTC
        Assert.True(LateRule.IsLate(checkIn, Sunday, NineAm, 10, plusThree));                          // 10:05 local > 09:10
        Assert.False(LateRule.IsLate(checkIn, Sunday, new TimeOnly(10, 0), 10, plusThree));            // 10:05 local ≤ 10:10
    }

    [Fact]
    public void A_zero_grace_makes_any_check_in_after_the_start_late()
    {
        var localCheckIn = new DateTime(2026, 10, 25, 9, 0, 1, DateTimeKind.Unspecified);

        Assert.True(LateRule.IsLate(new DateTimeOffset(localCheckIn, Cairo.GetUtcOffset(localCheckIn)), Sunday, NineAm, 0, Cairo));
    }
}
