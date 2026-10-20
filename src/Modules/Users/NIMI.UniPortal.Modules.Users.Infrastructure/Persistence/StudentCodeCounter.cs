namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;

/// <summary>Persistence-only row: the last number handed out for one code prefix (e.g. <c>CS-2026-#</c> → 42).</summary>
internal sealed class StudentCodeCounter
{
    public string CounterKey { get; set; } = null!;

    public int LastValue { get; set; }
}
