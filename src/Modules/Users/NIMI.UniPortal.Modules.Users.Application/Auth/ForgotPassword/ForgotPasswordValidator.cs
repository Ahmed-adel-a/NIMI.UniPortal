using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ForgotPassword;

internal sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(255).EmailAddress();
    }
}
