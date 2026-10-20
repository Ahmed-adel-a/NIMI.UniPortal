using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.UpdateDepartment;

internal sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(c => c.FacultyId).NotEmpty();
        RuleFor(c => c.Code).OrgCode();
        RuleFor(c => c.Name).OrgName();
        RuleFor(c => c.NameAr).OrgName();
        RuleFor(c => c.Description).MaximumLength(2000);
    }
}
