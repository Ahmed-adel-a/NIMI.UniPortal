using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListMyClasses;

/// <summary>Same ownership rule as the teaching schedule (R10): an active assignment for the whole subject or the group.</summary>
internal sealed class ListMyClassesHandler(ISchedulingDbContext db) : IRequestHandler<ListMyClassesQuery, IReadOnlyList<TeachingClassDto>>
{
    public async Task<IReadOnlyList<TeachingClassDto>> Handle(ListMyClassesQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsTeachingStaff)
            throw new ForbiddenException("Only teaching staff have classes.");

        return await db.SubjectGroups.AsNoTracking()
            .Where(g => g.IsActive && g.Term.Status == TermStatus.Active)
            .Where(g => db.TeachingAssignments.Any(a =>
                a.IsActive &&
                a.StaffId == query.StaffId &&
                a.SubjectId == g.SubjectId &&
                a.TermId == g.TermId &&
                (a.SubjectGroupId == null || a.SubjectGroupId == g.Id)))
            .OrderBy(g => g.Subject.Code)
            .ThenBy(g => g.GroupNumber)
            .Select(g => new TeachingClassDto(
                g.TermId, g.SubjectId, g.Subject.Code, g.Subject.Name, g.Subject.NameAr, g.Subject.CreditHours, g.Id, g.GroupNumber))
            .ToListAsync(cancellationToken);
    }
}
