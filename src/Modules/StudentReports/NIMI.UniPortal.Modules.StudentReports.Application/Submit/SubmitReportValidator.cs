using FluentValidation;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Submit;

internal sealed class SubmitReportValidator : AbstractValidator<SubmitReportCommand>
{
    public SubmitReportValidator()
    {
        RuleFor(c => c.Category)
            .Must(c => ReportNames.TryParse<ReportCategory>(c, out _))
            .WithMessage("Use academic, attendance, conduct or other.");
        RuleFor(c => c.Title).NotEmpty().MaximumLength(StudentReport.MaxTitleLength);
        RuleFor(c => c.Message).NotEmpty().MaximumLength(StudentReport.MaxTextLength);
    }
}
