using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities.CreateFacility;

internal sealed class CreateFacilityValidator : AbstractValidator<CreateFacilityCommand>
{
    public CreateFacilityValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.Capacity).InclusiveBetween(1, 5000);
        RuleFor(c => c.Building).MaximumLength(100);
        RuleFor(c => c.Floor).MaximumLength(20);
    }
}
