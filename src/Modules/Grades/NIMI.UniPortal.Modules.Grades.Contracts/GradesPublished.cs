using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.Grades.Contracts;

/// <summary>
/// Raised after grades are published or a published grade is corrected (P9.2). The lifecycle variant answers it:
/// credit ledger rows, completed/failed registrations and GPA snapshots. A correction re-publishes the one grade.
/// </summary>
public sealed record GradesPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TermId,
    IReadOnlyList<PublishedGrade> Grades) : IIntegrationEvent;
