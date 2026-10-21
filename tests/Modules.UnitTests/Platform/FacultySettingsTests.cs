// ACCEPTANCE TEST — written by the consultant for phase P1.4. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.UnitTests.Platform;

public sealed class FacultySettingsTests
{
    private static readonly DateTimeOffset Created = new(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Created.AddHours(1);
    private static readonly Guid Faculty = Guid.CreateVersion7();

    [Fact]
    public void A_new_override_row_inherits_everything()
    {
        var settings = FacultySettings.Create(Faculty, Created);

        Assert.NotEqual(Guid.Empty, settings.Id);
        Assert.Equal(7, settings.Id.Version);                                   // Guid v7
        Assert.Equal(Faculty, settings.FacultyId);
        Assert.Null(settings.WeekStartDay);
        Assert.Null(settings.WorkingDays);
        Assert.Null(settings.AttendanceThreshold);
        Assert.Null(settings.LateGraceMinutes);
        Assert.Null(settings.GradingScale);
        Assert.Null(settings.StudentCodeFormat);
        Assert.Null(settings.MilitaryEducationPolicy);
        Assert.False(settings.HasOverrides);
        Assert.Equal(Created, settings.CreatedAt);
        Assert.Null(settings.UpdatedAt);
    }

    [Fact]
    public void Overriding_stores_valid_values_normalized_and_stamps_the_time()
    {
        var settings = FacultySettings.Create(Faculty, Created);

        settings.Override(
            weekStartDay: 1,
            workingDays: [5, 1, 3],
            attendanceThreshold: 60m,
            lateGraceMinutes: 15,
            gradingScale: [new("F", 0m, 0m), new(" P ", 50m, 1m)],
            studentCodeFormat: "{FAC}{YEAR}{SEQ:000}",
            militaryEducationPolicy: new MilitaryEducationPolicy(MilitaryEducationMode.Mandatory),
            now: Later);

        Assert.Equal(1, settings.WeekStartDay);
        Assert.Equal([1, 3, 5], settings.WorkingDays);                          // sorted, like the university settings
        Assert.Equal(60m, settings.AttendanceThreshold);
        Assert.Equal(15, settings.LateGraceMinutes);
        Assert.Equal(["P", "F"], settings.GradingScale!.Select(b => b.Grade));  // highest band first, trimmed
        Assert.Equal("{FAC}{YEAR}{SEQ:000}", settings.StudentCodeFormat);
        Assert.Equal(MilitaryEducationMode.Mandatory, settings.MilitaryEducationPolicy!.Mode);
        Assert.True(settings.HasOverrides);
        Assert.Equal(Later, settings.UpdatedAt);
    }

    [Fact]
    public void Null_means_inherit_and_HasOverrides_reflects_it()
    {
        var settings = FacultySettings.Create(Faculty, Created);

        Override(settings, lateGraceMinutes: 20);
        Assert.True(settings.HasOverrides);
        Assert.Equal(20, settings.LateGraceMinutes);
        Assert.Null(settings.AttendanceThreshold);

        Override(settings, attendanceThreshold: 70m);                           // full replace: grace goes back to inherit
        Assert.Null(settings.LateGraceMinutes);
        Assert.Equal(70m, settings.AttendanceThreshold);

        Override(settings);                                                     // everything null
        Assert.False(settings.HasOverrides);
    }

    [Fact]
    public void Each_invalid_override_is_rejected_with_its_field_name()
    {
        AssertRejected("weekStartDay", s => Override(s, weekStartDay: 7));
        AssertRejected("workingDays", s => Override(s, workingDays: []));
        AssertRejected("workingDays", s => Override(s, workingDays: [0, 7]));
        AssertRejected("workingDays", s => Override(s, workingDays: [2, 2]));
        AssertRejected("attendanceThreshold", s => Override(s, attendanceThreshold: 100.01m));
        AssertRejected("attendanceThreshold", s => Override(s, attendanceThreshold: -1m));
        AssertRejected("lateGraceMinutes", s => Override(s, lateGraceMinutes: 181));
        AssertRejected("gradingScale", s => Override(s, gradingScale: []));
        AssertRejected("gradingScale", s => Override(s, gradingScale: [new("A", 90m, 4m)]));
        AssertRejected("studentCodeFormat", s => Override(s, studentCodeFormat: "{FAC}-{YEAR}"));
    }

    [Fact]
    public void A_rejected_override_leaves_the_previous_values_untouched()
    {
        var settings = FacultySettings.Create(Faculty, Created);
        Override(settings, attendanceThreshold: 60m);

        Assert.ThrowsAny<ArgumentException>(() => Override(settings, attendanceThreshold: 50m, lateGraceMinutes: 999));

        Assert.Equal(60m, settings.AttendanceThreshold);
        Assert.Null(settings.LateGraceMinutes);
    }

    private static void AssertRejected(string field, Action<FacultySettings> act)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => act(FacultySettings.Create(Faculty, Created)));
        Assert.Equal(field, error.ParamName);
    }

    /// <summary>Calls Override with every field null (inherit) except the ones the test sets.</summary>
    private static void Override(
        FacultySettings settings,
        int? weekStartDay = null,
        int[]? workingDays = null,
        decimal? attendanceThreshold = null,
        int? lateGraceMinutes = null,
        GradeBand[]? gradingScale = null,
        string? studentCodeFormat = null) =>
        settings.Override(
            weekStartDay, workingDays, attendanceThreshold, lateGraceMinutes,
            gradingScale, studentCodeFormat, militaryEducationPolicy: null, now: Later);
}
