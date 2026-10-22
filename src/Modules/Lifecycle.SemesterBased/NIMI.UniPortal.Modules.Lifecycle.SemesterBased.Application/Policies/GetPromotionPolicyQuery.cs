using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

/// <summary>The effective promotion policy of a faculty, or the university's when no faculty is given.</summary>
public sealed record GetPromotionPolicyQuery(Guid? FacultyId) : IQuery<PromotionPolicyDto>;
