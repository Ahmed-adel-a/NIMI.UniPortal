using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Refresh;

internal sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
