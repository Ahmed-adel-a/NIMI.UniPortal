using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Categories;

/// <summary>The categories the caller can use (Student Affairs: all, including inactive). A short list, so not paged.</summary>
public sealed record ListCategoriesQuery(Viewer Caller) : IQuery<IReadOnlyList<CategoryDto>>;
