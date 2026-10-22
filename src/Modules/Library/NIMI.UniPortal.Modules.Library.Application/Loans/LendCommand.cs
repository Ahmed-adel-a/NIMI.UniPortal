using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

/// <summary>Student Affairs lends a copy (by barcode) to a member under their role's rules.</summary>
public sealed record LendCommand(string? Barcode, Guid UserId) : ICommand<LoanDto>;
