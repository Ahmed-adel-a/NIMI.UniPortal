using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Receipts;

/// <summary>
/// Atomic per-year receipt allocation (rule R6): one upsert returns the new value, so two concurrent submissions never
/// share a number. It runs outside the command's save, so a failed save leaves a gap — never a duplicate.
/// </summary>
internal sealed class ReceiptAllocator(AssignmentsDbContext db) : IReceiptAllocator
{
    public async Task<int> NextAsync(int year, CancellationToken cancellationToken)
    {
        var values = await db.Database.SqlQuery<int>($"""
            INSERT INTO assignments.receipt_counters ("year", last_value)
            VALUES ({year}, 1)
            ON CONFLICT ("year") DO UPDATE
                SET last_value = assignments.receipt_counters.last_value + 1
            RETURNING last_value AS "Value"
            """)
            .ToListAsync(cancellationToken);

        return values[0];
    }
}
