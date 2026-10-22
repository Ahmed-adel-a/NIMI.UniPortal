using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ResetPasswordWithToken;

internal sealed class ResetPasswordWithTokenValidator : AbstractValidator<ResetPasswordWithTokenCommand>
{
    public ResetPasswordWithTokenValidator()
    {
        RuleFor(c => c.Token).NotEmpty().MaximumLength(200);
        RuleFor(c => c.NewPassword).StrongPassword();
    }
}
