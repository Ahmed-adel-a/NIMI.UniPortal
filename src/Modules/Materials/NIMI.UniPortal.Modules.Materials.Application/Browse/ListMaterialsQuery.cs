using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Materials.Application.Browse;

/// <summary>P7.2: one subject's materials this term, optionally by week or file type.</summary>
public sealed record ListMaterialsQuery(
    Guid? SubjectId,
    int? WeekNumber,
    string? Type,
    Viewer Caller,
    PageRequest Paging) : IQuery<PagedResult<MaterialDto>>;
