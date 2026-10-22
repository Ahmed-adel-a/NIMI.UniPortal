using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Categories;

internal sealed class ListCategoriesHandler(IForumDbContext db, ForumAccess access)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = db.Categories.AsNoTracking();
        if (!query.Caller.IsAdmin)
        {
            var facultyId = await access.FacultyOfAsync(query.Caller, cancellationToken);
            categories = categories.Where(c => c.IsActive && (c.FacultyId == null || c.FacultyId == facultyId));
        }

        var list = await categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return [.. list.Select(CategoryDto.From)];
    }
}
