using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetPaymentReport;

internal sealed class GetPaymentReportHandler(IPaymentsDbContext db, IUsersApi users)
    : IRequestHandler<GetPaymentReportQuery, PaymentReportDto>
{
    public async Task<PaymentReportDto> Handle(
        GetPaymentReportQuery query, CancellationToken cancellationToken)
    {
        Guid? effectiveFacultyId = query.FacultyId;
        Guid? effectiveDepartmentId = query.DepartmentId;

        if (query.DepartmentId is { } departmentId)
        {
            var department = await users.GetDepartmentAsync(departmentId, cancellationToken)
                ?? throw ValidationErrors.For("departmentId", "Unknown department.");

            effectiveFacultyId = department.FacultyId;

            if (query.FacultyId is { } facultyId && facultyId != department.FacultyId)
                throw ValidationErrors.For("departmentId", "The department belongs to another faculty.");
        }
        else if (query.FacultyId is { } facultyId)
        {
            _ = await users.GetFacultyAsync(facultyId, cancellationToken)
                ?? throw ValidationErrors.For("facultyId", "Unknown faculty.");
        }

        var items = await db.FeeItems
            .AsNoTracking()
            .Where(f => query.TermId == null || f.TermId == null || f.TermId == query.TermId)
            .ToListAsync(cancellationToken);

        bool ItemInScope(FeeItem f)
        {
            if (effectiveDepartmentId is { } department)
            {
                return f.DepartmentId == department
                       || (f.DepartmentId is null
                           && (f.FacultyId is null || f.FacultyId == effectiveFacultyId));
            }

            return query.FacultyId is not { } faculty || f.FacultyId is null || f.FacultyId == faculty;
        }

        var scopedItems = items.Where(ItemInScope).OrderBy(f => f.Name).ThenBy(f => f.Id).ToList();
        var itemIds = scopedItems.Select(f => f.Id).ToHashSet();

        var studentFees = await db.StudentFees
            .AsNoTracking()
            .Where(f => itemIds.Contains(f.FeeItemId))
            .Select(f => new { f.Id, f.FeeItemId, f.StudentId, f.AmountDue })
            .ToListAsync(cancellationToken);

        var studentFeeIds = studentFees.Select(f => f.Id).ToHashSet();
        var feeItemByStudentFee = studentFees.ToDictionary(f => f.Id, f => f.FeeItemId);

        var payments = await db.Payments
            .AsNoTracking()
            .Where(p => studentFeeIds.Contains(p.StudentFeeId))
            .Select(p => new { p.StudentFeeId, p.StudentId, p.Amount })
            .ToListAsync(cancellationToken);

        var scoped = effectiveDepartmentId is not null || query.FacultyId is not null;
        var placements = new Dictionary<Guid, StudentPlacementRef>();
        if (scoped)
        {
            var studentIds = studentFees.Select(f => f.StudentId)
                .Concat(payments.Select(p => p.StudentId))
                .Distinct();

            foreach (var studentId in studentIds)
            {
                if (await users.GetStudentPlacementAsync(studentId, cancellationToken) is { } placement)
                    placements[studentId] = placement;
            }
        }

        bool StudentInScope(Guid studentId)
        {
            if (!scoped)
                return true;

            return placements.TryGetValue(studentId, out var placement)
                   && (effectiveDepartmentId is { } department
                       ? placement.DepartmentId == department
                       : placement.FacultyId == query.FacultyId);
        }

        var counts = new Dictionary<(Guid?, Guid?), int>();

        async Task<int> CountAsync(Guid? facultyId, Guid? departmentId)
        {
            if (counts.TryGetValue((facultyId, departmentId), out var cached))
                return cached;

            var count = await users.CountActiveStudentsAsync(facultyId, departmentId, cancellationToken);
            counts[(facultyId, departmentId)] = count;
            return count;
        }

        async Task<int> ExpectedCountAsync(FeeItem item)
        {
            if (effectiveDepartmentId is not null)
                return await CountAsync(effectiveFacultyId, effectiveDepartmentId);

            if (item.DepartmentId is { } itemDepartment)
                return await CountAsync(item.FacultyId, itemDepartment);

            if (query.FacultyId is { } faculty)
                return await CountAsync(faculty, null);

            if (item.FacultyId is { } itemFaculty)
                return await CountAsync(itemFaculty, null);

            return await CountAsync(null, null);
        }

        var rows = new List<PaymentReportRowDto>();
        foreach (var item in scopedItems)
        {
            var expected = item.Category == FeeCategory.MilitaryEducation
                ? studentFees
                    .Where(f => f.FeeItemId == item.Id && StudentInScope(f.StudentId))
                    .Sum(f => f.AmountDue)
                : item.Amount * await ExpectedCountAsync(item);

            var collectedRows = payments
                .Where(p => feeItemByStudentFee[p.StudentFeeId] == item.Id && StudentInScope(p.StudentId))
                .ToList();

            var collected = collectedRows.Sum(p => p.Amount);
            var payers = collectedRows.Select(p => p.StudentId).Distinct().Count();

            rows.Add(new PaymentReportRowDto(
                item.Id,
                item.Name,
                item.NameAr,
                FeeCategoryNames.ToWire(item.Category),
                item.Amount,
                expected,
                collected,
                payers,
                expected - collected));
        }

        return new PaymentReportDto(rows);
    }
}
