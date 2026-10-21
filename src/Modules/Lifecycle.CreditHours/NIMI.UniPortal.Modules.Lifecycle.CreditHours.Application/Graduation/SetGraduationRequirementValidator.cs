using FluentValidation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Graduation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

internal sealed class SetGraduationRequirementValidator : AbstractValidator<SetGraduationRequirementCommand>
{
    public SetGraduationRequirementValidator()
    {
        RuleFor(c => c.RequiredCredits).InclusiveBetween(1, GraduationRequirement.MaxCredits);
    }
}
