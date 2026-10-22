using FluentValidation;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

internal sealed class OverridePromotionValidator : AbstractValidator<OverridePromotionCommand>
{
    public OverridePromotionValidator()
    {
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.Result)
            .IsInEnum()
            .NotEqual(PromotionResult.Held).WithMessage("Holding is decided by the fees, not by hand.");
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
    }
}
