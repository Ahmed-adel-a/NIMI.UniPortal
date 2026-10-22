using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Categories;

/// <summary>Student Affairs creates (<see cref="Id"/> null) or updates a category.</summary>
public sealed record SaveCategoryCommand(
    Guid? Id,
    string? Name,
    string? NameAr,
    Guid? FacultyId,
    int SortOrder,
    bool IsActive) : ICommand<CategoryDto>;
