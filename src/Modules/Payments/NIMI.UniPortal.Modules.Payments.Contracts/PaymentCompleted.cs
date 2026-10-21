using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.Payments.Contracts;

/// <summary>
/// Raised for office and online payments (P5.3/P5.7). The lifecycle variant confirms a pending military-education
/// registration when <see cref="Category"/> is <c>military_education</c> and <see cref="FeeFullyPaid"/> is true.
/// </summary>
public sealed record PaymentCompleted(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PaymentId,
    Guid StudentId,
    Guid FeeItemId,
    string Category,
    decimal Amount,
    bool FeeFullyPaid) : IIntegrationEvent;
