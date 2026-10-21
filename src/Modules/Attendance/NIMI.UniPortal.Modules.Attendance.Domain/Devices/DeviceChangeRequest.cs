namespace NIMI.UniPortal.Modules.Attendance.Domain.Devices;

/// <summary>
/// A student's request to replace their registered phone (decision DEVICE, R11). Student Affairs decides once;
/// approval swaps the active device in the handler.
/// </summary>
public sealed class DeviceChangeRequest
{
    private const int MinFingerprintLength = 8;
    private const int MaxFingerprintLength = 200;
    private const int MaxReasonLength = 500;

    private DeviceChangeRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public string NewFingerprint { get; private set; } = null!;

    public string Reason { get; private set; } = null!;

    public DeviceChangeStatus Status { get; private set; }

    public Guid? DecidedBy { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static DeviceChangeRequest Create(
        Guid studentId,
        string newFingerprint,
        string reason,
        DateTimeOffset now)
    {
        var fingerprint = newFingerprint?.Trim() ?? string.Empty;
        if (fingerprint.Length is < MinFingerprintLength or > MaxFingerprintLength)
            throw new ArgumentException(
                $"A device fingerprint must be between {MinFingerprintLength} and {MaxFingerprintLength} characters.",
                nameof(newFingerprint));

        var trimmedReason = reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length == 0)
            throw new ArgumentException("A reason is required.", nameof(reason));
        if (trimmedReason.Length > MaxReasonLength)
            throw new ArgumentException($"At most {MaxReasonLength} characters.", nameof(reason));

        return new DeviceChangeRequest
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            NewFingerprint = fingerprint,
            Reason = trimmedReason,
            Status = DeviceChangeStatus.Pending,
            CreatedAt = now,
        };
    }

    public void Approve(Guid decidedBy, DateTimeOffset now) => Decide(DeviceChangeStatus.Approved, decidedBy, now);

    public void Reject(Guid decidedBy, DateTimeOffset now) => Decide(DeviceChangeStatus.Rejected, decidedBy, now);

    private void Decide(DeviceChangeStatus status, Guid decidedBy, DateTimeOffset now)
    {
        if (Status != DeviceChangeStatus.Pending)
            throw new InvalidOperationException("This device change request has already been decided.");

        Status = status;
        DecidedBy = decidedBy;
        DecidedAt = now;
    }
}
