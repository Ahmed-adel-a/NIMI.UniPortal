using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Login;

internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(128);
    }
}
