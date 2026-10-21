using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.UpdateTerm;

internal sealed class UpdateTermValidator : AbstractValidator<UpdateTermCommand>
{
    public UpdateTermValidator()
    {
        RuleFor(c => c.AcademicYear).InclusiveBetween(2000, 2100);
        RuleFor(c => c.EndsOn).GreaterThan(c => c.StartsOn);
        RuleFor(c => c.WeekCount).InclusiveBetween(1, 30);
    }
}
