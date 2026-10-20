using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.CreateFaculty;

internal sealed class CreateFacultyValidator : AbstractValidator<CreateFacultyCommand>
{
    public CreateFacultyValidator()
    {
        RuleFor(c => c.Code).OrgCode();
        RuleFor(c => c.Name).OrgName();
        RuleFor(c => c.NameAr).OrgName();
    }
}
