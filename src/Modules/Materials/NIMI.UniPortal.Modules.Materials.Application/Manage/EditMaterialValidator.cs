using FluentValidation;

namespace NIMI.UniPortal.Modules.Materials.Application.Manage;

internal sealed class EditMaterialValidator : AbstractValidator<EditMaterialCommand>
{
    public EditMaterialValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.WeekNumber).InclusiveBetween(1, 30).When(c => c.WeekNumber is not null);
    }
}
