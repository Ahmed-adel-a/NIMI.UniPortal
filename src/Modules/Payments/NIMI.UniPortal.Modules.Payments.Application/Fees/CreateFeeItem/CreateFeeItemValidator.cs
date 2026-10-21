using FluentValidation;
using NIMI.UniPortal.Modules.Payments.Application.Common;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.CreateFeeItem;

internal sealed class CreateFeeItemValidator : AbstractValidator<CreateFeeItemCommand>
{
    public CreateFeeItemValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.NameAr).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.Category)
            .Must(value => FeeCategoryNames.TryParse(value, out _))
            .WithMessage("Unknown fee category.");
        RuleFor(c => c.Amount)
            .Must(value => value > 0 && value == decimal.Round(value, 2))
            .WithMessage("Must be positive with at most 2 decimals.");
    }
}
