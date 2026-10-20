using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;

/// <summary>
/// One atomic upsert per number: concurrent requests for the same prefix are serialized by PostgreSQL's row lock,
/// so two students can never get the same number (trap #3). Runs immediately, outside the command's commit.
/// </summary>
internal sealed class StudentCodeAllocator(UsersDbContext db) : IStudentCodeAllocator
{
    public async Task<int> NextAsync(string counterKey, CancellationToken cancellationToken)
    {
        var values = await db.Database.SqlQuery<int>($"""
            INSERT INTO users.student_code_counters (counter_key, last_value)
            VALUES ({counterKey}, 1)
            ON CONFLICT (counter_key) DO UPDATE SET last_value = student_code_counters.last_value + 1
            RETURNING last_value AS "Value"
            """).ToListAsync(cancellationToken);

        return values.Single();
    }
}
