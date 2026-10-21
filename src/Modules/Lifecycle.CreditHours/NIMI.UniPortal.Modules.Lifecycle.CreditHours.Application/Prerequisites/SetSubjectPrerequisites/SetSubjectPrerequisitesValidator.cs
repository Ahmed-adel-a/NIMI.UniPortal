using FluentValidation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.SetSubjectPrerequisites;

/// <summary>Shape checks only; unknown/self/cyclic prerequisites need the database and are checked in the handler.</summary>
internal sealed class SetSubjectPrerequisitesValidator : AbstractValidator<SetSubjectPrerequisitesCommand>
{
    public SetSubjectPrerequisitesValidator()
    {
        RuleFor(c => c.SubjectId).NotEmpty();
        RuleForEach(c => c.PrerequisiteSubjectIds).NotEmpty();
    }
}
