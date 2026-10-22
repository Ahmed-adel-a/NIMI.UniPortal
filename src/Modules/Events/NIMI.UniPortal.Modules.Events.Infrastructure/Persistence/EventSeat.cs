namespace NIMI.UniPortal.Modules.Events.Infrastructure.Persistence;

/// <summary>The seat counter row of an event with a capacity — written only by the atomic SQL in <c>EventSeats</c>.</summary>
internal sealed class EventSeat
{
    public Guid EventId { get; set; }

    public int Capacity { get; set; }

    public int Taken { get; set; }
}
