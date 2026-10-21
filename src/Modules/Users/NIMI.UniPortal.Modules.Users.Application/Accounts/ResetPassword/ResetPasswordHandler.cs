using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ResetPassword;

internal sealed class ResetPasswordHandler(
    IUsersDbContext db, IPasswordHasher hasher, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<ResetPasswordCommand, TemporaryPasswordDto>
{
    public async Task<TemporaryPasswordDto> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var temporaryPassword = TemporaryPassword.Generate();
        user.ResetPassword(hasher.Hash(temporaryPassword), clock.GetUtcNow());
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.UserPasswordReset,
            AuditEntities.User,
            user.Id,
            $"Password of {user.FullName} reset to a temporary one (must be changed at next login)."));
        return new TemporaryPasswordDto(temporaryPassword);
    }
}
