using FluentValidation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.AssignMilitaryEducation;

/// <summary>Shape checks only; group/term/subject and student lookups happen in the handler.</summary>
internal sealed class AssignMilitaryEducationValidator : AbstractValidator<AssignMilitaryEducationCommand>
{
    public AssignMilitaryEducationValidator()
    {
        RuleFor(c => c.StudentIds).NotEmpty();
        RuleFor(c => c.SubjectGroupId).NotEmpty();
    }
}
