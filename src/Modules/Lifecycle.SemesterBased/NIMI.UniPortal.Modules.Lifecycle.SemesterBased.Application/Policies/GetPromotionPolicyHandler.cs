using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

internal sealed class GetPromotionPolicyHandler(EffectivePolicies policies, IUsersApi users)
    : IRequestHandler<GetPromotionPolicyQuery, PromotionPolicyDto>
{
    public async Task<PromotionPolicyDto> Handle(GetPromotionPolicyQuery query, CancellationToken cancellationToken)
    {
        if (query.FacultyId is { } facultyId && await users.GetFacultyAsync(facultyId, cancellationToken) is null)
            throw new NotFoundException($"No faculty with id '{facultyId}'.");

        return await policies.ForAsync(query.FacultyId, cancellationToken);
    }
}
