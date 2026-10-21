// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using System.Buffers.Binary;
using System.Security.Cryptography;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

namespace NIMI.UniPortal.Modules.UnitTests.Attendance;

/// <summary>The rotating QR token (phase §5 + §7a): proof of being in the room now, valid 20 seconds.</summary>
public sealed class QrTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 3, 0, TimeSpan.Zero);
    private static readonly byte[] Secret = RandomNumberGenerator.GetBytes(32);
    private static readonly Guid Session = Guid.CreateVersion7();

    [Fact]
    public void A_fresh_token_is_valid_and_names_its_session_and_time()
    {
        var token = QrToken.Issue(Session, Secret, Now);

        Assert.Equal(QrTokenResult.Valid, QrToken.Verify(token, Session, Secret, Now));
        Assert.True(QrToken.TryRead(token, out var sessionId, out var issuedAt));
        Assert.Equal(Session, sessionId);
        Assert.Equal(Now, issuedAt);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void The_token_layout_is_session_then_time_then_signature()
    {
        var token = QrToken.Issue(Session, Secret, Now);
        var bytes = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/').PadRight((token.Length + 3) / 4 * 4, '='));

        Assert.Equal(56, bytes.Length);
        Assert.Equal(Session, new Guid(bytes.AsSpan(0, 16)));
        Assert.Equal(Now.ToUnixTimeSeconds(), BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(16, 8)));
        Assert.Equal(HMACSHA256.HashData(Secret, bytes.AsSpan(0, 24).ToArray()), bytes.AsSpan(24, 32).ToArray());
    }

    [Fact]
    public void A_token_lives_20_seconds_with_5_seconds_of_clock_skew()
    {
        var token = QrToken.Issue(Session, Secret, Now);

        Assert.Equal(QrTokenResult.Valid, QrToken.Verify(token, Session, Secret, Now.AddSeconds(20)));
        Assert.Equal(QrTokenResult.Expired, QrToken.Verify(token, Session, Secret, Now.AddSeconds(21)));
        Assert.Equal(QrTokenResult.Valid, QrToken.Verify(token, Session, Secret, Now.AddSeconds(-5)));
        Assert.Equal(QrTokenResult.Expired, QrToken.Verify(token, Session, Secret, Now.AddSeconds(-6)));
    }

    [Fact]
    public void A_token_for_another_session_or_signed_with_another_secret_is_refused()
    {
        var token = QrToken.Issue(Session, Secret, Now);

        Assert.Equal(QrTokenResult.WrongSession, QrToken.Verify(token, Guid.CreateVersion7(), Secret, Now));
        Assert.Equal(QrTokenResult.BadSignature, QrToken.Verify(token, Session, RandomNumberGenerator.GetBytes(32), Now));
    }

    [Fact]
    public void A_tampered_or_garbage_token_is_refused()
    {
        var token = QrToken.Issue(Session, Secret, Now);
        var middle = token.Length / 2;                                       // a character that carries only data bits
        var tampered = token[..middle] + (token[middle] == 'A' ? 'B' : 'A') + token[(middle + 1)..];

        Assert.NotEqual(QrTokenResult.Valid, QrToken.Verify(tampered, Session, Secret, Now));
        Assert.Equal(QrTokenResult.Malformed, QrToken.Verify("not-a-token", Session, Secret, Now));
        Assert.Equal(QrTokenResult.Malformed, QrToken.Verify("", Session, Secret, Now));
        Assert.False(QrToken.TryRead("not-a-token", out _, out _));
    }
}
