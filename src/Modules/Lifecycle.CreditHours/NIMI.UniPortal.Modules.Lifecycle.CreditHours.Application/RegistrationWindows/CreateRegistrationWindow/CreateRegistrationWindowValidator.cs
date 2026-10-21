using FluentValidation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.CreateRegistrationWindow;

/// <summary>Shape checks only; the window's date/credit invariants live in the domain.</summary>
internal sealed class CreateRegistrationWindowValidator : AbstractValidator<CreateRegistrationWindowCommand>
{
    public CreateRegistrationWindowValidator()
    {
        RuleFor(c => c.TermId).NotEmpty();
    }
}
