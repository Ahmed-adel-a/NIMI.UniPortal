using FluentValidation;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Inbox;

internal sealed class DepartmentReportsValidator : AbstractValidator<DepartmentReportsQuery>
{
    public DepartmentReportsValidator() =>
        RuleFor(q => q.Status)
            .Must(s => ReportNames.TryParse<ReportStatus>(s, out _))
            .When(q => !string.IsNullOrWhiteSpace(q.Status))
            .WithMessage("Use open, answered or closed.");
}
