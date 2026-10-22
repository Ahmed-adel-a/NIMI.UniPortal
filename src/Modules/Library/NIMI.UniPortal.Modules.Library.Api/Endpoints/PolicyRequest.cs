namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>PUT /api/admin/library/policies/{role}.</summary>
public sealed record PolicyRequest(int LoanDays, int MaxOpenLoans, int MaxRenewals, int HoldDays);
