using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ChangePassword;

internal sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(c => c.CurrentPassword).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
        RuleFor(c => c.NewPassword).StrongPassword()
            .NotEqual(c => c.CurrentPassword).WithMessage("Choose a password different from the current one.");
    }
}
