using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Application.Auth;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.SetUserStatus;

internal sealed class SetUserStatusHandler(IUsersDbContext db, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<SetUserStatusCommand, Unit>
{
    public async Task<Unit> Handle(SetUserStatusCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsActive && command.UserId == command.ActingAdminId)
            throw ValidationErrors.For("IsActive", "You can't deactivate your own account.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.SetActive(command.IsActive, clock.GetUtcNow());
        if (!command.IsActive)
            await Sessions.RevokeAllAsync(db, user.Id, clock.GetUtcNow(), cancellationToken);
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.UserStatusChanged,
            AuditEntities.User,
            user.Id,
            $"Account of {user.FullName} {(command.IsActive ? "activated" : "deactivated")}."));
        return Unit.Value;
    }
}
