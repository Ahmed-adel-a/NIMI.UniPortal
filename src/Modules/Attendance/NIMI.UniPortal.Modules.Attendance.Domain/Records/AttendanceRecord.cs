namespace NIMI.UniPortal.Modules.Attendance.Domain.Records;

/// <summary>
/// One student's attendance in one session. The database's unique (session, student) index is the security
/// boundary (R4); manual corrections stay audited with the professor and a mandatory note (R7).
/// </summary>
public sealed class AttendanceRecord
{
    private const int MinFingerprintLength = 8;
    private const int MaxFingerprintLength = 200;
    private const int MaxNoteLength = 500;

    private AttendanceRecord()
    {
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public Guid StudentId { get; private set; }

    public AttendanceStatus Status { get; private set; }

    public AttendanceMethod Method { get; private set; }

    /// <summary>Set only for QR check-ins; null for manual entries and auto-absent rows.</summary>
    public DateTimeOffset? CheckedInAt { get; private set; }

    public string? DeviceFingerprint { get; private set; }

    public Guid? RecordedBy { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Present, late and audited manual attendance all count towards the percentage (R9).</summary>
    public bool Attended => Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.ManualAudit;

    public static AttendanceRecord CheckIn(
        Guid sessionId,
        Guid studentId,
        bool isLate,
        string deviceFingerprint,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            StudentId = studentId,
            Status = isLate ? AttendanceStatus.Late : AttendanceStatus.Present,
            Method = AttendanceMethod.QrScan,
            CheckedInAt = now,
            DeviceFingerprint = RequiredFingerprint(deviceFingerprint, nameof(deviceFingerprint)),
            CreatedAt = now,
        };

    public static AttendanceRecord Manual(
        Guid sessionId,
        Guid studentId,
        AttendanceStatus requested,
        string note,
        Guid recordedBy,
        DateTimeOffset now)
    {
        var status = ManualStatus(requested);
        return new AttendanceRecord
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            StudentId = studentId,
            Status = status,
            Method = AttendanceMethod.ManualEntry,
            CheckedInAt = null,
            RecordedBy = recordedBy,
            Note = RequiredNote(note),
            CreatedAt = now,
        };
    }

    public void OverrideManually(
        AttendanceStatus requested,
        string note,
        Guid recordedBy,
        DateTimeOffset now)
    {
        Status = ManualStatus(requested);
        Method = AttendanceMethod.ManualEntry;
        CheckedInAt = null;
        DeviceFingerprint = null;
        RecordedBy = recordedBy;
        Note = RequiredNote(note);
        UpdatedAt = now;
    }

    public static AttendanceRecord AutoAbsent(Guid sessionId, Guid studentId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            StudentId = studentId,
            Status = AttendanceStatus.Absent,
            Method = AttendanceMethod.AutoAbsent,
            CheckedInAt = null,
            CreatedAt = now,
        };

    private static AttendanceStatus ManualStatus(AttendanceStatus requested) =>
        requested switch
        {
            AttendanceStatus.Present or AttendanceStatus.Late => AttendanceStatus.ManualAudit,
            AttendanceStatus.Absent => AttendanceStatus.Absent,
            _ => throw new ArgumentOutOfRangeException(
                nameof(requested),
                requested,
                "Manual attendance must be present, late or absent."),
        };

    private static string RequiredFingerprint(string value, string paramName)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length is < MinFingerprintLength or > MaxFingerprintLength)
            throw new ArgumentException(
                $"A device fingerprint must be between {MinFingerprintLength} and {MaxFingerprintLength} characters.",
                paramName);
        return trimmed;
    }

    private static string RequiredNote(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("A note is required.", "note");
        if (trimmed.Length > MaxNoteLength)
            throw new ArgumentException($"At most {MaxNoteLength} characters.", "note");
        return trimmed;
    }
}
