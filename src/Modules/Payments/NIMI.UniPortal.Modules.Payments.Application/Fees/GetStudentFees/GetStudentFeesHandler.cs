using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.GetStudentFees;

internal sealed class GetStudentFeesHandler(
    IPaymentsDbContext db,
    IUsersApi users,
    IPlatformSettingsApi platform,
    TimeProvider clock) : IRequestHandler<GetStudentFeesQuery, StudentFeesDto>
{
    public async Task<StudentFeesDto> Handle(GetStudentFeesQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsStudent)
            throw new ForbiddenException("Only students have fees.");

        var placement = await users.GetStudentPlacementAsync(query.StudentId, cancellationToken);
        if (placement is null || !placement.IsActive)
            throw new ForbiddenException("Only active students have fees.");

        var items = await db.FeeItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var existing = await db.StudentFees
            .AsNoTracking()
            .Where(f => f.StudentId == query.StudentId)
            .ToListAsync(cancellationToken);

        var byItem = existing.ToDictionary(f => f.FeeItemId);
        var settings = await platform.GetEffectiveSettingsAsync(placement.FacultyId, cancellationToken);
        var today = EffectiveTime.Today(clock.GetUtcNow(), settings.TimeZone);

        var lines = items
            .Where(f => (query.TermId is null || f.TermId is null || f.TermId == query.TermId)
                        && (byItem.ContainsKey(f.Id) || f.AppliesTo(placement.FacultyId, placement.DepartmentId)))
            .OrderBy(f => f.DueOn)
            .ThenBy(f => f.Name)
            .ThenBy(f => f.Id)
            .Select(f =>
            {
                byItem.TryGetValue(f.Id, out var paid);
                var amountDue = paid?.AmountDue ?? f.Amount;
                var amountPaid = paid?.AmountPaid ?? 0m;
                var balance = amountDue - amountPaid;

                return new StudentFeeLineDto(
                    f.Id,
                    f.Name,
                    f.NameAr,
                    FeeCategoryNames.ToWire(f.Category),
                    amountDue,
                    amountPaid,
                    balance,
                    EnumWire.ToWire(paid?.Status ?? StudentFeeStatus.Pending),
                    f.DueOn,
                    balance > 0 && f.DueOn < today);
            })
            .ToList();

        return new StudentFeesDto(lines, lines.Sum(l => l.Balance));
    }
}
