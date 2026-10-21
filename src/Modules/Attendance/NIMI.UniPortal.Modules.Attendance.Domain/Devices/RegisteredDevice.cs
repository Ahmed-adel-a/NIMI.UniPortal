namespace NIMI.UniPortal.Modules.Attendance.Domain.Devices;

/// <summary>
/// The one active phone a student may check in from (decision DEVICE). Revoking keeps the row as history; the
/// partial unique index on student_id where revoked_at is null enforces "one active phone".
/// </summary>
public sealed class RegisteredDevice
{
    private const int MinFingerprintLength = 8;
    private const int MaxFingerprintLength = 200;

    private RegisteredDevice()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public string Fingerprint { get; private set; } = null!;

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static RegisteredDevice Register(Guid studentId, string fingerprint, DateTimeOffset now)
    {
        var trimmed = fingerprint?.Trim() ?? string.Empty;
        if (trimmed.Length is < MinFingerprintLength or > MaxFingerprintLength)
            throw new ArgumentException(
                $"A device fingerprint must be between {MinFingerprintLength} and {MaxFingerprintLength} characters.",
                "deviceFingerprint");

        return new RegisteredDevice
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            Fingerprint = trimmed,
            RegisteredAt = now,
        };
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
