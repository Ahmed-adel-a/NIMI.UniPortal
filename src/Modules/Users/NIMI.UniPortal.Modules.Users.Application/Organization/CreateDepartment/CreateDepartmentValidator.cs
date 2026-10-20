using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.CreateDepartment;

internal sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator()
    {
        RuleFor(c => c.FacultyId).NotEmpty();
        RuleFor(c => c.Code).OrgCode();
        RuleFor(c => c.Name).OrgName();
        RuleFor(c => c.NameAr).OrgName();
        RuleFor(c => c.Description).MaximumLength(2000);
    }
}
