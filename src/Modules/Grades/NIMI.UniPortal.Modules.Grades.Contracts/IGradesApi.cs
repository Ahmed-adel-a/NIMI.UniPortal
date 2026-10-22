namespace NIMI.UniPortal.Modules.Grades.Contracts;

/// <summary>What other modules (and the host's dashboards, P10.6) may read from Grades.</summary>
public interface IGradesApi
{
    /// <summary>Grades of a term still waiting for Student Affairs, optionally only of the given students.</summary>
    Task<GradeBacklog> GetBacklogAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken = default);
}
