namespace NIMI.UniPortal.Hosting.DemoData;

/// <summary>
/// The lifecycle half of the demo campus. Rosters belong to the host's one lifecycle variant (decision ENROLL), so
/// the host registers this to place <see cref="DemoCampus.Placements"/> its own way; it runs before the teaching
/// content is posted, so students receive those notifications.
/// </summary>
public interface IDemoEnrollment
{
    Task EnrollAsync(DemoCampus campus, CancellationToken cancellationToken);
}
