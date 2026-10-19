using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.UnitTests.Platform;

public sealed class SystemSettingsTests
{
    private static readonly DateTimeOffset Created = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Created.AddDays(1);

    [Fact]
    public void Default_row_has_the_singleton_id_and_cairo_calendar()
    {
        var settings = SystemSettings.CreateDefault(Created);

        Assert.Equal(SystemSettings.SingletonId, settings.Id);
        Assert.Equal("Africa/Cairo", settings.TimeZone);
        Assert.Equal(0, settings.WeekStartDay);                       // Saturday
        Assert.Equal([0, 1, 2, 3, 4, 5], settings.WorkingDays);       // Saturday – Thursday
        Assert.Equal(75m, settings.AttendanceThreshold);
        Assert.Contains(settings.GradingScale, b => b.MinScore == 0m);
        Assert.Null(settings.UpdatedAt);
    }

    [Fact]
    public void Update_with_valid_values_normalizes_and_stamps_the_time()
    {
        var settings = SystemSettings.CreateDefault(Created);

        Update(settings, primaryColor: "#ab12cd", workingDays: [3, 1, 2], gradingScale:
        [
            new("F", 0m, 0m), new("A", 90m, 4m), new(" B ", 75m, 3m),
        ]);

        Assert.Equal("#AB12CD", settings.PrimaryColor);
        Assert.Equal([1, 2, 3], settings.WorkingDays);
        Assert.Equal(["A", "B", "F"], settings.GradingScale.Select(b => b.Grade));   // highest band first, trimmed
        Assert.Equal(Later, settings.UpdatedAt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(120)]
    public void Attendance_threshold_outside_0_to_100_is_rejected(double threshold) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Update(SystemSettings.CreateDefault(Created), attendanceThreshold: (decimal)threshold));

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Week_start_day_outside_0_to_6_is_rejected(int day) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Update(SystemSettings.CreateDefault(Created), weekStartDay: day));

    [Fact]
    public void Working_days_must_be_present_in_range_and_unique()
    {
        var settings = SystemSettings.CreateDefault(Created);

        Assert.Throws<ArgumentException>(() => Update(settings, workingDays: []));
        Assert.Throws<ArgumentOutOfRangeException>(() => Update(settings, workingDays: [0, 7]));
        Assert.Throws<ArgumentException>(() => Update(settings, workingDays: [1, 1]));
    }

    [Theory]
    [InlineData("0A4CB3")]
    [InlineData("#0A4CB")]
    [InlineData("#GGGGGG")]
    [InlineData("blue")]
    public void Colors_must_be_hex_rgb(string color) =>
        Assert.Throws<ArgumentException>(() => Update(SystemSettings.CreateDefault(Created), primaryColor: color));

    [Theory]
    [InlineData("Cairo")]
    [InlineData("Not/AZone")]
    [InlineData("")]
    public void Timezone_must_be_an_IANA_id(string zone) =>
        Assert.Throws<ArgumentException>(() => Update(SystemSettings.CreateDefault(Created), timeZone: zone));

    [Fact]
    public void Grading_scale_must_cover_every_score_and_be_unambiguous()
    {
        var settings = SystemSettings.CreateDefault(Created);

        Assert.Throws<ArgumentException>(() => Update(settings, gradingScale: []));
        Assert.Throws<ArgumentException>(() => Update(settings, gradingScale: [new("A", 90m, 4m)]));                      // no band from 0
        Assert.Throws<ArgumentException>(() => Update(settings, gradingScale: [new("A", 0m, 4m), new("a", 50m, 3m)]));    // duplicate grade
        Assert.Throws<ArgumentException>(() => Update(settings, gradingScale: [new("A", 0m, 4m), new("B", 0m, 3m)]));     // same minimum
        Assert.Throws<ArgumentOutOfRangeException>(() => Update(settings, gradingScale: [new("A", 0m, 6m)]));             // points > 5
    }

    [Fact]
    public void Student_code_format_needs_a_sequence_token()
    {
        Assert.Throws<ArgumentException>(() => Update(SystemSettings.CreateDefault(Created), studentCodeFormat: "{DEPT}-{YEAR}"));
    }

    [Fact]
    public void A_rejected_update_reports_the_offending_field()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Update(SystemSettings.CreateDefault(Created), lateGraceMinutes: 500));

        Assert.Equal("lateGraceMinutes", error.ParamName);
    }

    /// <summary>Calls Update with the defaults, overriding only what the test is about.</summary>
    private static void Update(
        SystemSettings settings,
        string? primaryColor = null,
        string? timeZone = null,
        int? weekStartDay = null,
        int[]? workingDays = null,
        decimal? attendanceThreshold = null,
        int? lateGraceMinutes = null,
        GradeBand[]? gradingScale = null,
        string? studentCodeFormat = null) =>
        settings.Update(
            universityName: "Cairo University",
            universityNameAr: "جامعة القاهرة",
            domain: null,
            logoUrl: null,
            primaryColor: primaryColor ?? "#0A4CB3",
            secondaryColor: "#5B9BFF",
            timeZone: timeZone ?? "Africa/Cairo",
            weekStartDay: weekStartDay ?? 0,
            workingDays: workingDays ?? [0, 1, 2, 3, 4, 5],
            attendanceThreshold: attendanceThreshold ?? 75m,
            lateGraceMinutes: lateGraceMinutes ?? 10,
            gradingScale: gradingScale ?? [.. SystemSettings.DefaultGradingScale],
            studentCodeFormat: studentCodeFormat ?? "{DEPT}-{YEAR}-{SEQ:0000}",
            militaryEducationPolicy: new MilitaryEducationPolicy(MilitaryEducationMode.Mandatory),
            updatedAt: Later);
}
