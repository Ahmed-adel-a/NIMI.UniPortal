using System.Security.Cryptography;

namespace NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

/// <summary>
/// One lecture session opened by the teaching professor (P6.1). Its secret signs the rotating QR tokens and is
/// generated once per session; closing is idempotent and keeps the first close time.
/// </summary>
public sealed class AttendanceSession
{
    private AttendanceSession()
    {
    }

    public Guid Id { get; private set; }

    public Guid SlotId { get; private set; }

    public Guid SubjectGroupId { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid TermId { get; private set; }

    public DateOnly SessionDate { get; private set; }

    public Guid OpenedBy { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>32 random bytes, never exposed by the HTTP API — only the derived token is.</summary>
    public byte[] QrSecret { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsOpen => ClosedAt is null;

    public static AttendanceSession Open(
        Guid slotId,
        Guid subjectGroupId,
        Guid subjectId,
        Guid termId,
        DateOnly sessionDate,
        Guid openedBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SlotId = slotId,
            SubjectGroupId = subjectGroupId,
            SubjectId = subjectId,
            TermId = termId,
            SessionDate = sessionDate,
            OpenedBy = openedBy,
            OpenedAt = now,
            QrSecret = RandomNumberGenerator.GetBytes(32),
            CreatedAt = now,
        };

    public void Close(DateTimeOffset now) => ClosedAt ??= now;
}
