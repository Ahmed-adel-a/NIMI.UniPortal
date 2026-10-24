using FluentValidation;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Oversight;

internal sealed class AllReportsValidator : AbstractValidator<AllReportsQuery>
{
    public AllReportsValidator() =>
        RuleFor(q => q.Status)
            .Must(s => ReportNames.TryParse<ReportStatus>(s, out _))
            .When(q => !string.IsNullOrWhiteSpace(q.Status))
            .WithMessage("Use open, answered or closed.");
}
