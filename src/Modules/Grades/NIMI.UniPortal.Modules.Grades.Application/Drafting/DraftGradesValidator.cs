using FluentValidation;

namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

internal sealed class DraftGradesValidator : AbstractValidator<DraftGradesCommand>
{
    public DraftGradesValidator()
    {
        RuleFor(c => c.Entries).NotEmpty().WithMessage("At least one grade entry.");
        RuleForEach(c => c.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.StudentId).NotEmpty();
            entry.RuleFor(e => e.Score)
                .InclusiveBetween(0m, 100m)
                .Must(score => decimal.Round(score, 2) == score)
                .WithMessage("Between 0 and 100, at most 2 decimals.");
        });
    }
}
