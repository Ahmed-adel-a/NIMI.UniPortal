using FluentValidation;

namespace NIMI.UniPortal.Modules.Grades.Application.Publishing;

internal sealed class PublishGradesValidator : AbstractValidator<PublishGradesCommand>
{
    public PublishGradesValidator()
    {
        RuleFor(c => c.TermId).NotEmpty().WithMessage("Choose the term to publish.");
    }
}
