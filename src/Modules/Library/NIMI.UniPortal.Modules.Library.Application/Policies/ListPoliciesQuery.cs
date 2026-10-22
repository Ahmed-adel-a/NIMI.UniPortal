using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Policies;

/// <summary>The loan rules of every role (saved values, or the product defaults until changed).</summary>
public sealed record ListPoliciesQuery : IQuery<IReadOnlyList<PolicyDto>>;
