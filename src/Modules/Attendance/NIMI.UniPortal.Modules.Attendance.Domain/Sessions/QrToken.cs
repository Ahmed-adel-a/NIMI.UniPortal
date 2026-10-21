using System.Buffers.Binary;
using System.Security.Cryptography;

namespace NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

/// <summary>
/// The rotating attendance QR token (phase §5): session id (16 bytes) ‖ issued-at unix seconds (8 bytes) ‖
/// HMAC-SHA256 over the first 24 bytes (32 bytes), base64url without padding. A token is a proof of being in the
/// room now, valid for 20 seconds with 5 seconds of clock skew — it carries no student data.
/// </summary>
public static class QrToken
{
    public const int LifetimeSeconds = 20;

    private const int SessionBytes = 16;
    private const int TimeBytes = 8;
    private const int SignedBytes = SessionBytes + TimeBytes;
    private const int SignatureBytes = 32;
    private const int TokenBytes = SignedBytes + SignatureBytes;
    private const int ClockSkewSeconds = 5;

    public static string Issue(Guid sessionId, byte[] secret, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(secret);

        Span<byte> bytes = stackalloc byte[TokenBytes];
        sessionId.TryWriteBytes(bytes[..SessionBytes]);
        BinaryPrimitives.WriteInt64BigEndian(bytes.Slice(SessionBytes, TimeBytes), now.ToUnixTimeSeconds());
        HMACSHA256.HashData(secret, bytes[..SignedBytes], bytes[SignedBytes..]);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>Reads the session and issue time without checking the signature; used only to find the session first.</summary>
    public static bool TryRead(string token, out Guid sessionId, out DateTimeOffset issuedAt)
    {
        sessionId = Guid.Empty;
        issuedAt = default;

        if (!TryDecode(token, out var bytes))
            return false;

        sessionId = new Guid(bytes.AsSpan(0, SessionBytes));

        try
        {
            issuedAt = DateTimeOffset.FromUnixTimeSeconds(
                BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(SessionBytes, TimeBytes)));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>Verify checks in the pinned order: malformed → wrong session → bad signature → expired.</summary>
    public static QrTokenResult Verify(string token, Guid sessionId, byte[] secret, DateTimeOffset now)
    {
        if (!TryDecode(token, out var bytes))
            return QrTokenResult.Malformed;

        if (new Guid(bytes.AsSpan(0, SessionBytes)) != sessionId)
            return QrTokenResult.WrongSession;

        Span<byte> expected = stackalloc byte[SignatureBytes];
        HMACSHA256.HashData(secret, bytes.AsSpan(0, SignedBytes), expected);

        if (!CryptographicOperations.FixedTimeEquals(expected, bytes.AsSpan(SignedBytes, SignatureBytes)))
            return QrTokenResult.BadSignature;

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(
            BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(SessionBytes, TimeBytes)));

        return now >= issuedAt.AddSeconds(-ClockSkewSeconds) && now <= issuedAt.AddSeconds(LifetimeSeconds)
            ? QrTokenResult.Valid
            : QrTokenResult.Expired;
    }

    private static bool TryDecode(string token, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrEmpty(token))
            return false;

        // Strict base64url: anything else is malformed before it can become a signature question.
        foreach (var character in token)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_')
                return false;
        }

        var base64 = token.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight((base64.Length + 3) / 4 * 4, '=');

        var buffer = new byte[TokenBytes];
        if (!Convert.TryFromBase64String(base64, buffer, out var written) || written != TokenBytes)
            return false;

        bytes = buffer;
        return true;
    }
}
