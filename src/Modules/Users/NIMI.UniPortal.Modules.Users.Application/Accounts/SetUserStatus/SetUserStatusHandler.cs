using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.SetUserStatus;

internal sealed class SetUserStatusHandler(IUsersDbContext db, TimeProvider clock) : IRequestHandler<SetUserStatusCommand, Unit>
{
    public async Task<Unit> Handle(SetUserStatusCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsActive && command.UserId == command.ActingAdminId)
            throw ValidationErrors.For("IsActive", "You can't deactivate your own account.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.SetActive(command.IsActive, clock.GetUtcNow());
        return Unit.Value;
    }
}
