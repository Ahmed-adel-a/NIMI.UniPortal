using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Policies;

internal sealed class UpdatePolicyHandler(ILibraryDbContext db, TimeProvider clock) : IRequestHandler<UpdatePolicyCommand, PolicyDto>
{
    public async Task<PolicyDto> Handle(UpdatePolicyCommand command, CancellationToken cancellationToken)
    {
        var role = command.Role.Trim().ToLowerInvariant();
        if (!LoanPolicy.Roles.Contains(role))
            throw new NotFoundException("Loan rules exist for students and professors.");

        var now = clock.GetUtcNow();
        var policy = await db.Policies.SingleOrDefaultAsync(p => p.Role == role, cancellationToken);
        if (policy is null)
        {
            policy = LoanPolicy.Default(role, now);
            db.Policies.Add(policy);
        }

        policy.Update(command.LoanDays, command.MaxOpenLoans, command.MaxRenewals, command.HoldDays, now);
        return PolicyDto.From(policy);
    }
}
