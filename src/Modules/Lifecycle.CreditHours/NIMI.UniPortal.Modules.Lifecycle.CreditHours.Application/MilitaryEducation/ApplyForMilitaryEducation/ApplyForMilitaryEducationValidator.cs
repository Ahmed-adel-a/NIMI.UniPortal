using FluentValidation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ApplyForMilitaryEducation;

/// <summary>Shape check only; the group, term, policy and duplicate checks need Scheduling/Platform/the database.</summary>
internal sealed class ApplyForMilitaryEducationValidator : AbstractValidator<ApplyForMilitaryEducationCommand>
{
    public ApplyForMilitaryEducationValidator()
    {
        RuleFor(c => c.SubjectGroupId).NotEmpty();
    }
}
