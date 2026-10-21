using FluentValidation;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.OpenSession;

internal sealed class OpenSessionValidator : AbstractValidator<OpenSessionCommand>
{
    public OpenSessionValidator()
    {
        RuleFor(c => c.SlotId).NotEmpty();
        RuleFor(c => c.OpenedBy).NotEmpty();
    }
}
