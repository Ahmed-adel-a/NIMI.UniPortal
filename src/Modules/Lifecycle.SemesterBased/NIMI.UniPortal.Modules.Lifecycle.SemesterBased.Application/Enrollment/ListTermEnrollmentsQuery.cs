using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

/// <summary>P4.8: enrollments filtered by term, student or group, paged.</summary>
public sealed record ListTermEnrollmentsQuery(Guid? TermId, Guid? StudentId, Guid? SubjectGroupId, PageRequest Paging)
    : IQuery<PagedResult<TermEnrollmentDto>>;
