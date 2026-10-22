using FluentValidation;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

internal sealed class SaveStudyPlanValidator : AbstractValidator<SaveStudyPlanCommand>
{
    public SaveStudyPlanValidator()
    {
        RuleFor(c => c.Levels).InclusiveBetween(1, AcademicLevels.Max);
        RuleFor(c => c.Subjects).NotNull();
        RuleForEach(c => c.Subjects).ChildRules(entry =>
        {
            entry.RuleFor(e => e.SubjectId).NotEmpty();
            entry.RuleFor(e => e.Level).InclusiveBetween(1, AcademicLevels.Max);
            entry.RuleFor(e => e.Term).IsInEnum();
        });
    }
}
