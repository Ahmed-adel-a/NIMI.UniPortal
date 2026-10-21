namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// The atomic seat counter of one subject group (P4.3, rule R8). It is written only by raw SQL
/// (<c>UPDATE … SET taken = taken + 1 WHERE taken &lt; capacity</c>) so concurrent registrations can never
/// over-book; the capacity is refreshed from Scheduling's <c>GroupRef.Capacity</c> on every attempt.
/// </summary>
public sealed class GroupSeat
{
    private GroupSeat()
    {
    }

    public Guid SubjectGroupId { get; private set; }

    public int Capacity { get; private set; }

    public int Taken { get; private set; }
}
