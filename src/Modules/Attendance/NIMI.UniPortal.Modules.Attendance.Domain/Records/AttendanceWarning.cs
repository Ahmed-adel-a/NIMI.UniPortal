namespace NIMI.UniPortal.Modules.Attendance.Domain.Records;

/// <summary>
/// Rule R10 marker: this student already received the warning for this group's current below-threshold run.
/// It is removed once the student is back at or above the effective threshold, so the next drop warns again.
/// </summary>
public sealed class AttendanceWarning
{
    private AttendanceWarning()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid SubjectGroupId { get; private set; }

    public DateTimeOffset WarnedAt { get; private set; }

    public static AttendanceWarning Create(Guid studentId, Guid subjectGroupId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            SubjectGroupId = subjectGroupId,
            WarnedAt = now,
        };
}
