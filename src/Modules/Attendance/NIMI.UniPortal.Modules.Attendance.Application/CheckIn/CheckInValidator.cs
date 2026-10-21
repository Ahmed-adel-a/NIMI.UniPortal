using FluentValidation;

namespace NIMI.UniPortal.Modules.Attendance.Application.CheckIn;

internal sealed class CheckInValidator : AbstractValidator<CheckInCommand>
{
    public CheckInValidator()
    {
        RuleFor(c => c.Token).NotEmpty();
        RuleFor(c => c.DeviceFingerprint).NotEmpty().Length(8, 200);
        RuleFor(c => c.StudentId).NotEmpty();
    }
}
