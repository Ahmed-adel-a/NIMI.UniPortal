namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>
/// The host's clock in tests: real time plus an offset tests can push forward (e.g. "15 minutes later" for lockout).
/// Reset to real time before every test, so tokens issued later aren't "from the future" for the JWT validator.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private TimeSpan _offset;

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + _offset;

    public void Advance(TimeSpan by) => _offset += by;

    public void Reset() => _offset = TimeSpan.Zero;
}
