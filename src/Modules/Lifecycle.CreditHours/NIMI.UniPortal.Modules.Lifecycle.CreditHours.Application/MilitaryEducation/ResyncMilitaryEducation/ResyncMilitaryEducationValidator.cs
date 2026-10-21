using FluentValidation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ResyncMilitaryEducation;

/// <summary>Shape check only; the term lookup happens in the handler.</summary>
internal sealed class ResyncMilitaryEducationValidator : AbstractValidator<ResyncMilitaryEducationCommand>
{
    public ResyncMilitaryEducationValidator()
    {
        RuleFor(c => c.TermId).NotEmpty();
    }
}
