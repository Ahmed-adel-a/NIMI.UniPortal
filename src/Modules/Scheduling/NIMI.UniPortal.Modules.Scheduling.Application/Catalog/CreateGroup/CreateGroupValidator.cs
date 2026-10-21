using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateGroup;

internal sealed class CreateGroupValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupValidator()
    {
        RuleFor(c => c.TermId).NotEmpty();
        RuleFor(c => c.GroupNumber).GreaterThan(0);
        RuleFor(c => c.Capacity).InclusiveBetween(1, 1000);
    }
}
