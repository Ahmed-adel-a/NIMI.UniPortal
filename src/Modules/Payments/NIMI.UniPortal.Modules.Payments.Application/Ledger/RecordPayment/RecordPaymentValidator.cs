using FluentValidation;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.RecordPayment;

internal sealed class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.FeeItemId).NotEmpty();
        RuleFor(c => c.Amount)
            .Must(value => value > 0 && value == decimal.Round(value, 2))
            .WithMessage("Must be positive with at most 2 decimals.");
        RuleFor(c => c.Method).NotEmpty();
        RuleFor(c => c.Reference).MaximumLength(100);
    }
}
