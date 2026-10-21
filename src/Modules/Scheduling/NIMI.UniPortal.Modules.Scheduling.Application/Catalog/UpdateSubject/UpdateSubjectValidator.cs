using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateSubject;

internal sealed class UpdateSubjectValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.NameAr).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.CreditHours).InclusiveBetween(0, 12);
        RuleFor(c => c.Category).IsInEnum();
    }
}
