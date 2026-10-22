using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

public sealed record OverridePromotionRequest(Guid StudentId, PromotionResult Result, string? Reason);
