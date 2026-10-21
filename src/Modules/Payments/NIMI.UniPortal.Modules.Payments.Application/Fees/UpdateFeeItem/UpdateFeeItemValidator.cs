using FluentValidation;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.UpdateFeeItem;

internal sealed class UpdateFeeItemValidator : AbstractValidator<UpdateFeeItemCommand>
{
    public UpdateFeeItemValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.NameAr).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.Amount)
            .Must(value => value > 0 && value == decimal.Round(value, 2))
            .WithMessage("Must be positive with at most 2 decimals.");
    }
}
