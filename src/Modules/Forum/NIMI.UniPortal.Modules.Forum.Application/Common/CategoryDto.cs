using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>A forum category; <see cref="FacultyId"/> null means university-wide.</summary>
public sealed record CategoryDto(Guid Id, Guid? FacultyId, string Name, string NameAr, int SortOrder, bool IsActive)
{
    public static CategoryDto From(ForumCategory category) =>
        new(category.Id, category.FacultyId, category.Name, category.NameAr, category.SortOrder, category.IsActive);
}
