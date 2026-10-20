using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth;

/// <summary>What a user-chosen password must look like: 8–128 characters with at least one letter and one digit.</summary>
internal static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(MinLength)
            .MaximumLength(MaxLength)
            .Must(p => p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage("Use at least one letter and one digit.");
}
