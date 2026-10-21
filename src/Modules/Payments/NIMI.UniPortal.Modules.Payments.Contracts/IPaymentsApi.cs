namespace NIMI.UniPortal.Modules.Payments.Contracts;

/// <summary>What other modules (and the host's dashboards, P10) may read from Payments.</summary>
public interface IPaymentsApi
{
    /// <summary>The student's total outstanding balance over every applicable fee (0 when nothing is due).</summary>
    Task<decimal> GetOutstandingBalanceAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Collected and still outstanding totals of the expected-vs-collected report (P5.6) for a term, faculty or
    /// department: the Student Affairs dashboard (P10.6).
    /// </summary>
    Task<FeeCollection> GetCollectionAsync(
        Guid? termId, Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default);
}
