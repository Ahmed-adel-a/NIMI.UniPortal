using FluentValidation;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Answer;

internal sealed class AnswerReportValidator : AbstractValidator<AnswerReportCommand>
{
    public AnswerReportValidator()
    {
        RuleFor(c => c.Reply)
            .NotEmpty().WithMessage("Write a reply, close the report, or both.")
            .When(c => !c.Close);
        RuleFor(c => c.Reply!)
            .Must(r => !string.IsNullOrWhiteSpace(r)).WithMessage("The reply is empty.")
            .MaximumLength(StudentReport.MaxTextLength)
            .When(c => c.Reply is not null);
    }
}
