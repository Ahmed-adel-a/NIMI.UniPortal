using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListTeachingAssignments;

internal sealed class ListTeachingAssignmentsHandler(ISchedulingDbContext db, IUsersApi users)
    : IRequestHandler<ListTeachingAssignmentsQuery, PagedResult<TeachingAssignmentDto>>
{
    public async Task<PagedResult<TeachingAssignmentDto>> Handle(
        ListTeachingAssignmentsQuery query, CancellationToken cancellationToken)
    {
        var assignments = db.TeachingAssignments.AsNoTracking()
            .Where(a => query.TermId == null || a.TermId == query.TermId)
            .Where(a => query.StaffId == null || a.StaffId == query.StaffId)
            .Where(a => query.SubjectId == null || a.SubjectId == query.SubjectId);

        var total = await assignments.CountAsync(cancellationToken);
        var rows = await assignments
            .OrderBy(a => a.SubjectId)
            .ThenBy(a => a.StaffId)
            .ThenBy(a => a.SubjectGroupId)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(a => new
            {
                a.Id,
                a.StaffId,
                a.SubjectId,
                SubjectCode = a.Subject.Code,
                a.TermId,
                a.SubjectGroupId,
                GroupNumber = a.SubjectGroup != null ? a.SubjectGroup.GroupNumber : (int?)null,
                a.Role,
                a.IsActive,
            })
            .ToListAsync(cancellationToken);

        var names = new Dictionary<Guid, string>();
        foreach (var staffId in rows.Select(r => r.StaffId).Distinct())
        {
            var user = await users.GetUserAsync(staffId, cancellationToken);
            names[staffId] = user?.FullName ?? string.Empty;
        }

        var items = rows
            .Select(r => new TeachingAssignmentDto(
                r.Id,
                r.StaffId,
                names[r.StaffId],
                r.SubjectId,
                r.SubjectCode,
                r.TermId,
                r.SubjectGroupId,
                r.GroupNumber,
                r.Role,
                r.IsActive))
            .ToList();

        return new PagedResult<TeachingAssignmentDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
