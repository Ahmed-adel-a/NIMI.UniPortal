using FluentValidation;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Application.Reports;

internal sealed class FileReportValidator : AbstractValidator<FileReportCommand>
{
    public FileReportValidator()
    {
        RuleFor(c => c.ThreadId)
            .Must((command, threadId) => (threadId is null) != (command.PostId is null))
            .WithMessage("Report exactly one thread or one post.");
        RuleFor(c => c.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Say why you are reporting it.")
            .MaximumLength(ForumReport.MaxReasonLength);
    }
}
