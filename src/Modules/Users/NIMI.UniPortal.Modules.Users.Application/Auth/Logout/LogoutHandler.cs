using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Logout;

internal sealed class LogoutHandler(IUsersDbContext db, TimeProvider clock) : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var hash = OpaqueToken.Hash(command.RefreshToken.Trim());
        var familyId = await db.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);
        if (familyId is { } family)
            await Sessions.RevokeFamilyAsync(db, family, clock.GetUtcNow(), cancellationToken);
        return Unit.Value;
    }
}
