using FluentValidation;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.ManualEntry;

internal sealed class ManualEntryValidator : AbstractValidator<ManualEntryCommand>
{
    public ManualEntryValidator()
    {
        RuleFor(c => c.SessionId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.Status).NotEmpty();
        RuleFor(c => c.Note).NotEmpty().MaximumLength(500);
    }
}
