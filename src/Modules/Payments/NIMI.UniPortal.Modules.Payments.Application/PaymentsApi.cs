using NIMI.UniPortal.Modules.Payments.Application.Fees.GetStudentFees;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.GetPaymentReport;
using NIMI.UniPortal.Modules.Payments.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application;

/// <summary>The Payments contract, answered by the student's own fees query (targeting rules in one place).</summary>
internal sealed class PaymentsApi(ISender sender) : IPaymentsApi
{
    public async Task<decimal> GetOutstandingBalanceAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetStudentFeesQuery(studentId, true, null), cancellationToken)).TotalBalance;

    public async Task<FeeCollection> GetCollectionAsync(
        Guid? termId, Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var report = await sender.Send(new GetPaymentReportQuery(termId, facultyId, departmentId), cancellationToken);
        return new FeeCollection(report.Items.Sum(i => i.Collected), report.Items.Sum(i => i.Outstanding));
    }
}
