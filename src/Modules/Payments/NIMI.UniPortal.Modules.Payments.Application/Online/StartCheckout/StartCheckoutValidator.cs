using FluentValidation;

namespace NIMI.UniPortal.Modules.Payments.Application.Online.StartCheckout;

internal sealed class StartCheckoutValidator : AbstractValidator<StartCheckoutCommand>
{
    public StartCheckoutValidator()
    {
        RuleFor(c => c.FeeItemId).NotEmpty();
        RuleFor(c => c.Amount)
            .Must(value => value > 0 && value == decimal.Round(value, 2))
            .WithMessage("Must be positive with at most 2 decimals.");
    }
}
