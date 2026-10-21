// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

namespace NIMI.UniPortal.Modules.UnitTests.Attendance;

/// <summary>Sessions and the records inside them (rules R4, R7, R8, R9).</summary>
public sealed class AttendanceRecordTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 7, 0, 0, TimeSpan.Zero);
    private static readonly Guid Session = Guid.CreateVersion7();
    private static readonly Guid Student = Guid.CreateVersion7();
    private static readonly Guid Professor = Guid.CreateVersion7();

    [Fact]
    public void A_session_opens_with_a_fresh_secret_and_closes_once()
    {
        var session = AttendanceSession.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), new DateOnly(2026, 10, 25), Professor, Now);
        var other = AttendanceSession.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), new DateOnly(2026, 10, 25), Professor, Now);

        Assert.Equal(7, session.Id.Version);
        Assert.True(session.IsOpen);
        Assert.Equal(32, session.QrSecret.Length);
        Assert.NotEqual(session.QrSecret, other.QrSecret);

        session.Close(Now.AddMinutes(90));
        session.Close(Now.AddMinutes(95));

        Assert.False(session.IsOpen);
        Assert.Equal(Now.AddMinutes(90), session.ClosedAt);
    }

    [Fact]
    public void A_qr_check_in_is_present_or_late_and_remembers_the_device()
    {
        var onTime = AttendanceRecord.CheckIn(Session, Student, isLate: false, "device-fingerprint-1", Now);
        var late = AttendanceRecord.CheckIn(Session, Guid.CreateVersion7(), isLate: true, "device-fingerprint-2", Now);

        Assert.Equal(AttendanceStatus.Present, onTime.Status);
        Assert.Equal(AttendanceMethod.QrScan, onTime.Method);
        Assert.Equal("device-fingerprint-1", onTime.DeviceFingerprint);
        Assert.Equal(Now, onTime.CheckedInAt);
        Assert.Equal(AttendanceStatus.Late, late.Status);
        Assert.True(onTime.Attended);
        Assert.True(late.Attended);
    }

    [Fact]
    public void Manual_entries_are_audited_and_need_a_note()
    {
        var present = AttendanceRecord.Manual(Session, Student, AttendanceStatus.Present, "Phone battery died", Professor, Now);
        var absent = AttendanceRecord.Manual(Session, Guid.CreateVersion7(), AttendanceStatus.Absent, "Left early", Professor, Now);

        Assert.Equal(AttendanceStatus.ManualAudit, present.Status);
        Assert.Equal(AttendanceMethod.ManualEntry, present.Method);
        Assert.Equal(Professor, present.RecordedBy);
        Assert.Equal("Phone battery died", present.Note);
        Assert.True(present.Attended);
        Assert.Equal(AttendanceStatus.Absent, absent.Status);
        Assert.False(absent.Attended);
        Assert.Equal("note", Assert.ThrowsAny<ArgumentException>(() =>
            AttendanceRecord.Manual(Session, Student, AttendanceStatus.Present, " ", Professor, Now)).ParamName);
        Assert.Equal("note", Assert.ThrowsAny<ArgumentException>(() =>
            AttendanceRecord.Manual(Session, Student, AttendanceStatus.Present, new string('n', 501), Professor, Now)).ParamName);
    }

    [Fact]
    public void A_teacher_can_override_a_scanned_record_with_a_note()
    {
        var record = AttendanceRecord.CheckIn(Session, Student, isLate: true, "device-fingerprint-1", Now);

        record.OverrideManually(AttendanceStatus.Present, "Traffic accident on the ring road", Professor, Now.AddMinutes(5));

        Assert.Equal(AttendanceStatus.ManualAudit, record.Status);
        Assert.Equal(AttendanceMethod.ManualEntry, record.Method);
        Assert.Equal("Traffic accident on the ring road", record.Note);
        Assert.Equal(Professor, record.RecordedBy);
    }

    [Fact]
    public void Closing_marks_missing_students_absent()
    {
        var record = AttendanceRecord.AutoAbsent(Session, Student, Now);

        Assert.Equal(AttendanceStatus.Absent, record.Status);
        Assert.Equal(AttendanceMethod.AutoAbsent, record.Method);
        Assert.Null(record.CheckedInAt);
        Assert.False(record.Attended);
    }

    [Theory]
    [InlineData(3, 4, 75.00)]
    [InlineData(2, 3, 66.67)]
    [InlineData(0, 5, 0.00)]
    [InlineData(0, 0, 100.00)]
    public void Attendance_rates_are_percentages_with_two_decimals(int attended, int held, double expected)
    {
        Assert.Equal((decimal)expected, AttendanceRate.Percentage(attended, held));
    }
}
