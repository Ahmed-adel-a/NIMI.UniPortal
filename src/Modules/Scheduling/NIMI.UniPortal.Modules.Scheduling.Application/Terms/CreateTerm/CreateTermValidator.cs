using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CreateTerm;

internal sealed class CreateTermValidator : AbstractValidator<CreateTermCommand>
{
    public CreateTermValidator()
    {
        RuleFor(c => c.AcademicYear).InclusiveBetween(2000, 2100);
        RuleFor(c => c.EndsOn).GreaterThan(c => c.StartsOn);
        RuleFor(c => c.WeekCount).InclusiveBetween(1, 30);
    }
}
