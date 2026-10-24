using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.TestAccounts;

internal sealed class SetTestAccountPasswordHandler(
    IUsersDbContext db, IPasswordHasher hasher, IHostEnvironment environment, TimeProvider clock)
    : IRequestHandler<SetTestAccountPasswordCommand, Unit>
{
    public async Task<Unit> Handle(SetTestAccountPasswordCommand command, CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
            throw new ForbiddenException("Test accounts are not available in Production.");
        if (string.IsNullOrEmpty(command.Password))
            throw ValidationErrors.For("password", "Required.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        user.ChangePassword(hasher.Hash(command.Password), clock.GetUtcNow());
        return Unit.Value;
    }
}
