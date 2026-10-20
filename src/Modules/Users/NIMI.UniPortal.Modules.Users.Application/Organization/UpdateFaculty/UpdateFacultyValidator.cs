using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.UpdateFaculty;

internal sealed class UpdateFacultyValidator : AbstractValidator<UpdateFacultyCommand>
{
    public UpdateFacultyValidator()
    {
        RuleFor(c => c.Code).OrgCode();
        RuleFor(c => c.Name).OrgName();
        RuleFor(c => c.NameAr).OrgName();
    }
}
