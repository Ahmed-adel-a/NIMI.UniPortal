using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Forum.Application.Categories;

internal sealed class SaveCategoryHandler(IForumDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<SaveCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(SaveCategoryCommand command, CancellationToken cancellationToken)
    {
        if (command.FacultyId is { } facultyId && await users.GetFacultyAsync(facultyId, cancellationToken) is null)
            throw ValidationErrors.For("facultyId", "Unknown faculty.");

        var now = clock.GetUtcNow();
        if (command.Id is not { } id)
        {
            var created = ForumCategory.Create(command.Name ?? string.Empty, command.NameAr ?? string.Empty, command.FacultyId, command.SortOrder, now);
            db.Categories.Add(created);
            return CategoryDto.From(created);
        }

        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        category.Update(command.Name ?? string.Empty, command.NameAr ?? string.Empty, command.FacultyId, command.SortOrder, command.IsActive, now);
        return CategoryDto.From(category);
    }
}
