using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateSubject;

internal sealed class CreateSubjectValidator : AbstractValidator<CreateSubjectCommand>
{
    public CreateSubjectValidator()
    {
        RuleFor(c => c.DepartmentId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.NameAr).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.CreditHours).InclusiveBetween(0, 12);
        RuleFor(c => c.Category).IsInEnum();
    }
}
