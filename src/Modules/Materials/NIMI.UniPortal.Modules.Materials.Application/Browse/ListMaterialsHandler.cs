using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.Modules.Materials.Domain;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Materials.Application.Browse;

/// <summary>
/// P7.2 — teachers of the subject and Student Affairs see everything; a registered student sees the subject-wide items
/// and their own group's (R2); anyone else is refused. Ordered by week (undated last), then newest first.
/// </summary>
internal sealed class ListMaterialsHandler(IMaterialsDbContext db, CourseAccess access, ISchedulingApi scheduling)
    : IRequestHandler<ListMaterialsQuery, PagedResult<MaterialDto>>
{
    public async Task<PagedResult<MaterialDto>> Handle(ListMaterialsQuery query, CancellationToken cancellationToken)
    {
        if (query.SubjectId is not { } subjectId)
            throw ValidationErrors.For("subjectId", "Choose a subject.");

        MaterialFileType? type = null;
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            if (!Enum.TryParse<MaterialFileType>(query.Type, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                throw ValidationErrors.For("type", "Type must be pdf, ppt, doc, xls, zip, image, video or other.");
            type = parsed;
        }

        var subject = await scheduling.GetSubjectAsync(subjectId, cancellationToken)
            ?? throw new NotFoundException("Subject not found.");
        var term = await access.ActiveTermAsync(cancellationToken);

        var materials = db.Materials.AsNoTracking()
            .Where(m => m.SubjectId == subject.Id && m.TermId == term.Id && !m.IsDeleted);

        if (!query.Caller.IsAdmin)
        {
            if (query.Caller.IsProfessor && await access.TeachesSubjectAsync(query.Caller.UserId, subject.Id, term.Id, cancellationToken))
            {
                // teaching staff see every item of the subject, group-targeted ones included
            }
            else if (query.Caller.IsStudent &&
                     await access.StudentGroupsAsync(query.Caller.UserId, subject.Id, term.Id, cancellationToken) is { Count: > 0 } groups)
            {
                materials = materials.Where(m => m.SubjectGroupId == null || groups.Contains(m.SubjectGroupId.Value));
            }
            else
            {
                throw new ForbiddenException("You are not registered in or teaching this subject.");
            }
        }

        if (query.WeekNumber is { } week)
            materials = materials.Where(m => m.WeekNumber == week);
        if (type is { } fileType)
            materials = materials.Where(m => m.FileType == fileType);

        var total = await materials.CountAsync(cancellationToken);
        var page = await materials
            .OrderBy(m => m.WeekNumber == null)
            .ThenBy(m => m.WeekNumber)
            .ThenByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MaterialDto>(
            [.. page.Select(m => MaterialDto.From(m, subject.Code))],
            query.Paging.Page,
            query.Paging.PageSize,
            total);
    }
}
