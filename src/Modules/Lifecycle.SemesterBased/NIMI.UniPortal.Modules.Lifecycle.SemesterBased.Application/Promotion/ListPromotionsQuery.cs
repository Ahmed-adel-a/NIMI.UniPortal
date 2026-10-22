using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>P4.9: year-end decisions, newest first, filtered by year, department, student or result.</summary>
public sealed record ListPromotionsQuery(
    int? AcademicYear, Guid? DepartmentId, Guid? StudentId, PromotionResult? Result, PageRequest Paging)
    : IQuery<PagedResult<PromotionDto>>;
