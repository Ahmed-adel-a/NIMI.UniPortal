using FluentValidation;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Login;

internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Role)
            .Must(r => UserRoles.TryParse(r, out _))
            .When(c => !string.IsNullOrWhiteSpace(c.Role))
            .WithMessage("Use student, professor, department_head or admin.");
    }
}
