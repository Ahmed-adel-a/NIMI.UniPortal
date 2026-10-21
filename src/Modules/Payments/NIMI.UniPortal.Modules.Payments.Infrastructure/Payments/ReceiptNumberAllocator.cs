using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Payments;

/// <summary>
/// Atomic per-year receipt allocation (rule R3): the upsert returns the new value in one statement, so two
/// concurrent payments can never receive the same number. Same pattern as the student-code counter.
/// </summary>
internal sealed class ReceiptNumberAllocator(PaymentsDbContext db) : IReceiptNumberAllocator
{
    public async Task<int> NextAsync(int year, CancellationToken cancellationToken)
    {
        var values = await db.Database.SqlQuery<int>($"""
            INSERT INTO payments.receipt_counters ("year", last_value)
            VALUES ({year}, 1)
            ON CONFLICT ("year") DO UPDATE
                SET last_value = payments.receipt_counters.last_value + 1
            RETURNING last_value AS "Value"
            """)
            .ToListAsync(cancellationToken);

        return values[0];
    }
}
