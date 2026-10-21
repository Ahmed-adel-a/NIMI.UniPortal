using FluentValidation;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateFacultySettings;

/// <summary>
/// Shape checks with one message per field (P1.4). The domain re-checks the same academic rules as the last line
/// of defence (and reports the same field names), so a missing rule here can never store invalid data.
/// </summary>
internal sealed class UpdateFacultySettingsValidator : AbstractValidator<UpdateFacultySettingsCommand>
{
    public UpdateFacultySettingsValidator()
    {
        RuleFor(c => c.WeekStartDay)
            .InclusiveBetween(0, 6)
            .WithMessage("0 = Saturday … 6 = Friday.")
            .When(c => c.WeekStartDay is not null);

        RuleFor(c => c.WorkingDays)
            .NotEmpty()
            .WithMessage("At least one working day.")
            .When(c => c.WorkingDays is not null);
        RuleForEach(c => c.WorkingDays)
            .InclusiveBetween(0, 6)
            .When(c => c.WorkingDays is not null);
        RuleFor(c => c.WorkingDays)
            .Must(days => days is null || days.Distinct().Count() == days.Length)
            .WithMessage("Working days must not repeat.")
            .When(c => c.WorkingDays is not null);

        RuleFor(c => c.AttendanceThreshold)
            .InclusiveBetween(0m, 100m)
            .When(c => c.AttendanceThreshold is not null);
        RuleFor(c => c.LateGraceMinutes)
            .InclusiveBetween(0, 180)
            .When(c => c.LateGraceMinutes is not null);

        RuleFor(c => c.GradingScale)
            .NotEmpty()
            .WithMessage("At least one grading band.")
            .When(c => c.GradingScale is not null);
        RuleForEach(c => c.GradingScale)
            .ChildRules(band =>
            {
                band.RuleFor(b => b.Grade).NotEmpty().MaximumLength(10);
                band.RuleFor(b => b.MinScore).InclusiveBetween(0m, 100m);
                band.RuleFor(b => b.Points).InclusiveBetween(0m, 5m);
            })
            .When(c => c.GradingScale is not null);

        RuleFor(c => c.StudentCodeFormat)
            .Must(format => format!.Contains("{SEQ", StringComparison.Ordinal))
            .WithMessage("The student-code format must contain a {SEQ} token so codes are unique.")
            .When(c => c.StudentCodeFormat is not null);

        RuleFor(c => c.MilitaryEducationPolicy)
            .Must(policy => policy is null || Enum.IsDefined(policy.Mode))
            .WithMessage("Unknown military-education mode.")
            .When(c => c.MilitaryEducationPolicy is not null);

        RuleFor(c => c.Version).NotEqual(0u).When(c => c.Version is not null);
    }
}
