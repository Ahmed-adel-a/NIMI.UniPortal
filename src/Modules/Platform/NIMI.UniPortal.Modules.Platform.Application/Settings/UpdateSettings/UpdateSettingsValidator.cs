using FluentValidation;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateSettings;

/// <summary>
/// Shape checks with one message per field (so the admin sees every problem at once).
/// The domain re-checks the business rules (IANA timezone, grading-scale coverage …) as the last line of defense.
/// </summary>
internal sealed class UpdateSettingsValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsValidator()
    {
        RuleFor(c => c.UniversityName).NotEmpty().MaximumLength(255);
        RuleFor(c => c.UniversityNameAr).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Domain).MaximumLength(255);
        RuleFor(c => c.LogoUrl).MaximumLength(500);
        RuleFor(c => c.PrimaryColor).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Must be a #RRGGBB color.");
        RuleFor(c => c.SecondaryColor).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Must be a #RRGGBB color.");
        RuleFor(c => c.TimeZone).NotEmpty().MaximumLength(64);
        RuleFor(c => c.WeekStartDay).InclusiveBetween(0, 6).WithMessage("0 = Saturday … 6 = Friday.");
        RuleFor(c => c.WorkingDays).NotEmpty()
            .Must(days => days.All(d => d is >= 0 and <= 6)).WithMessage("Days are 0 = Saturday … 6 = Friday.")
            .Must(days => days.Distinct().Count() == days.Length).WithMessage("Working days must not repeat.");
        RuleFor(c => c.AttendanceThreshold).InclusiveBetween(0m, 100m);
        RuleFor(c => c.LateGraceMinutes).InclusiveBetween(0, 180);
        RuleFor(c => c.GradingScale).NotEmpty();
        RuleForEach(c => c.GradingScale).ChildRules(band =>
        {
            band.RuleFor(b => b.Grade).NotEmpty().MaximumLength(5);
            band.RuleFor(b => b.MinScore).InclusiveBetween(0m, 100m);
            band.RuleFor(b => b.Points).InclusiveBetween(0m, 5m);
        });
        RuleFor(c => c.StudentCodeFormat).NotEmpty().MaximumLength(100)
            .Must(f => f.Contains("{SEQ", StringComparison.Ordinal)).WithMessage("Must contain a {SEQ} token so codes are unique.");
        RuleFor(c => c.MilitaryEducationPolicy).NotNull();
        RuleFor(c => c.MilitaryEducationPolicy.Mode).IsInEnum().When(c => c.MilitaryEducationPolicy is not null);
        RuleFor(c => c.Version).NotEqual(0u).WithMessage("Send the version you loaded (GET /api/admin/settings).");
    }
}
