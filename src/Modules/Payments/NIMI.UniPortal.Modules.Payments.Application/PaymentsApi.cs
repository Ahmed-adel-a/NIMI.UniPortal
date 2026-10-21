using NIMI.UniPortal.Modules.Payments.Application.Fees.GetStudentFees;
using NIMI.UniPortal.Modules.Payments.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application;

/// <summary>The Payments contract, answered by the student's own fees query (targeting rules in one place).</summary>
internal sealed class PaymentsApi(ISender sender) : IPaymentsApi
{
    public async Task<decimal> GetOutstandingBalanceAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetStudentFeesQuery(studentId, true, null), cancellationToken)).TotalBalance;
}
