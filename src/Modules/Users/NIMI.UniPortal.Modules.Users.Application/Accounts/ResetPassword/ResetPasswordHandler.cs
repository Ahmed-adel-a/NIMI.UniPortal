using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ResetPassword;

internal sealed class ResetPasswordHandler(IUsersDbContext db, IPasswordHasher hasher, TimeProvider clock)
    : IRequestHandler<ResetPasswordCommand, TemporaryPasswordDto>
{
    public async Task<TemporaryPasswordDto> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var temporaryPassword = TemporaryPassword.Generate();
        user.ResetPassword(hasher.Hash(temporaryPassword), clock.GetUtcNow());
        return new TemporaryPasswordDto(temporaryPassword);
    }
}
