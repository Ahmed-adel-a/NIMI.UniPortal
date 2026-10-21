using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateGroup;

internal sealed class UpdateGroupValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupValidator()
    {
        RuleFor(c => c.Capacity).InclusiveBetween(1, 1000);
    }
}
