namespace NIMI.UniPortal.Modules.Payments.Contracts;

/// <summary>What other modules (and the host's dashboards, P10) may read from Payments.</summary>
public interface IPaymentsApi
{
    /// <summary>The student's total outstanding balance over every applicable fee (0 when nothing is due).</summary>
    Task<decimal> GetOutstandingBalanceAsync(Guid studentId, CancellationToken cancellationToken = default);
}
