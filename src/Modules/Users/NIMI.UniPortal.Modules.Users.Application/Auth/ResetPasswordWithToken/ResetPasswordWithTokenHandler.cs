using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ResetPasswordWithToken;

internal sealed class ResetPasswordWithTokenHandler(
    IUsersDbContext db, IPasswordHasher hasher, IEventCollector events, TimeProvider clock)
    : IRequestHandler<ResetPasswordWithTokenCommand, Unit>
{
    private const string Invalid = "This reset link is invalid or has expired. Ask for a new one.";

    public async Task<Unit> Handle(ResetPasswordWithTokenCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = OpaqueToken.Hash(command.Token.Trim());
        var token = await db.PasswordResetTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null || !token.IsUsable(now))
            throw ValidationErrors.For("token", Invalid);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == token.UserId && u.IsActive, cancellationToken)
            ?? throw ValidationErrors.For("token", Invalid);

        user.ResetPasswordByEmail(hasher.Hash(command.NewPassword), now);
        var open = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.EndedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var other in open)
            other.End(now);
        await Sessions.RevokeAllAsync(db, user.Id, now, cancellationToken);

        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), now, user.Id, AuditActions.UserPasswordReset, AuditEntities.User, user.Id,
            $"{user.FullName} chose a new password through an emailed reset link."));
        return Unit.Value;
    }
}
