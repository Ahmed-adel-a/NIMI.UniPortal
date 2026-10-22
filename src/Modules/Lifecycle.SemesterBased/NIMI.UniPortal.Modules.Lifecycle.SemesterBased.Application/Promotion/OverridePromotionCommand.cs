using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>
/// P4.9: Student Affairs decides a student's year instead of the rule (promoted, promoted with carried subjects,
/// repeat, external or graduated) with a reason. Recorded with who decided, audited, and the student is notified.
/// </summary>
public sealed record OverridePromotionCommand(Guid StudentId, PromotionResult Result, string? Reason, Guid AdminId)
    : ICommand<PromotionDto>;
