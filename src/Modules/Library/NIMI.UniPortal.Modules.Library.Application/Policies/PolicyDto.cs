using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Application.Policies;

/// <summary>One role's loan rules.</summary>
public sealed record PolicyDto(string Role, int LoanDays, int MaxOpenLoans, int MaxRenewals, int HoldDays)
{
    public static PolicyDto From(LoanPolicy p) => new(p.Role, p.LoanDays, p.MaxOpenLoans, p.MaxRenewals, p.HoldDays);
}
