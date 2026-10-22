using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Logout;

internal sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
