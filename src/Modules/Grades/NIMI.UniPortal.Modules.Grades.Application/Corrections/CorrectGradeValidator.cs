using FluentValidation;
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Application.Corrections;

internal sealed class CorrectGradeValidator : AbstractValidator<CorrectGradeCommand>
{
    public CorrectGradeValidator()
    {
        RuleFor(c => c.Score)
            .InclusiveBetween(0m, 100m)
            .Must(score => decimal.Round(score, 2) == score)
            .WithMessage("Between 0 and 100, at most 2 decimals.");
        RuleFor(c => c.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("A correction needs a reason.")
            .MaximumLength(GradeCorrection.MaxReasonLength);
    }
}
