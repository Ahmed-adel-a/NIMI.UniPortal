using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Policies;

internal sealed class ListPoliciesHandler(LoanPolicies policies) : IRequestHandler<ListPoliciesQuery, IReadOnlyList<PolicyDto>>
{
    public async Task<IReadOnlyList<PolicyDto>> Handle(ListPoliciesQuery query, CancellationToken cancellationToken)
    {
        var list = new List<PolicyDto>(LoanPolicy.Roles.Count);
        foreach (var role in LoanPolicy.Roles)
            list.Add(PolicyDto.From(await policies.ForRoleAsync(role, cancellationToken)));
        return list;
    }
}
