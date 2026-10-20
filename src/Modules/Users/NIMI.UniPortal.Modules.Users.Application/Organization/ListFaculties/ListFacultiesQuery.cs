using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.ListFaculties;

/// <summary>Faculties ordered by code; inactive ones only on request.</summary>
public sealed record ListFacultiesQuery(bool IncludeInactive, PageRequest Paging) : IQuery<PagedResult<FacultyDto>>;
