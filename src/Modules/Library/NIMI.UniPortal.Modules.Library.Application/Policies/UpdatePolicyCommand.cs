using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Policies;

/// <summary>Student Affairs changes one role's loan rules; the next loan uses them.</summary>
public sealed record UpdatePolicyCommand(string Role, int LoanDays, int MaxOpenLoans, int MaxRenewals, int HoldDays) : ICommand<PolicyDto>;
