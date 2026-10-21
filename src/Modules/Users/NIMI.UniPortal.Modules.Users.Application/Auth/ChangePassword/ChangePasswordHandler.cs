using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ChangePassword;

internal sealed class ChangePasswordHandler(IUsersDbContext db, IPasswordHasher hasher, IAccessTokenIssuer tokens, TimeProvider clock)
    : IRequestHandler<ChangePasswordCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId && u.IsActive, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!hasher.Verify(user.PasswordHash, command.CurrentPassword))
            throw ValidationErrors.For(nameof(command.CurrentPassword), "The current password is incorrect.");

        user.ChangePassword(hasher.Hash(command.NewPassword), clock.GetUtcNow());

        var token = tokens.Issue(user);
        return new TokenResponse(token.Value, token.ExpiresAt, user.MustChangePassword);
    }
}
